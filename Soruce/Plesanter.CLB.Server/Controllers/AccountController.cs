using Codeer.LowCode.Blazor.Utils;
using Dapper;
using Plesanter.CLB.Client;
using Plesanter.CLB.Server.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Plesanter.CLB.Server.Controllers
{
    [ApiController, AutoValidateAntiforgeryToken]
    [Route("api/account")]
    public class AccountController : ControllerBase
    {
        readonly DataService _dataService;

        public AccountController(DataService dataService)
        {
            _dataService = dataService;
        }

        [Authorize]
        [HttpGet("current_user")]
        public StringWrapper GetCurrentUser()
            => new(DataService.GetCurrentUserId(HttpContext));

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginInfo? loginInfo)
        {
            if (loginInfo == null) throw new ArgumentException(nameof(loginInfo));

            if (!AuthenticateUser(loginInfo.Id??string.Empty, loginInfo.Password ?? string.Empty)) return Unauthorized();

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, loginInfo.Id ?? string.Empty)
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties { IsPersistent = loginInfo.IsPersistent });

            return Ok();
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok();
        }

        public bool AuthenticateUser(string loginId, string password)
        {
            var conn = _dataService.DbAccess.GetConnection("Pleasanter");

            var passwordHash = Sha512(password);

            var count = conn.ExecuteScalar<long>(
               @"SELECT COUNT(*) FROM ""Users""
                      WHERE lower(""LoginId"") = lower(@LoginId)
                        AND ""Password"" = @Password
                        AND ""Disabled"" = false",
               new { LoginId = loginId, Password = passwordHash });

            return count > 0;
        }
        
        static string Sha512(string input)
        {
            using var sha512 = SHA512.Create();
            var bytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }
}
