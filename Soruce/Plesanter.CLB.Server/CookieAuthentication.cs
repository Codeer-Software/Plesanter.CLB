using Dapper;
using Plesanter.CLB.Server.Services;
using Plesanter.CLB.Server.Shared;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Plesanter.CLB.Server
{
    public static class CookieAuthentication
    {
        public static void UseCookieAuthentication(this WebApplicationBuilder builder)
        {
            builder.Services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Events.OnRedirectToLogin = context =>
                    {
                        context.Response.StatusCode = 401;
                        return Task.CompletedTask;
                    };
                    options.Events.OnRedirectToAccessDenied = context =>
                    {
                        context.Response.StatusCode = 403;
                        return Task.CompletedTask;
                    };
                });

            //CSRF
            builder.Services.AddAntiforgery(options =>
            {
                options.HeaderName = "X-ANTIFORGERY-TOKEN";
            });
        }

        public static void UseCookieAuthentication(this WebApplication app)
        {
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseAntiforgery();
            app.Use(async (ctx, next) =>
            {
                if (HttpMethods.IsGet(ctx.Request.Method))
                {
                    if (ctx.Request.Headers.Accept.Any(a => a?.Contains("text/html") == true))
                    {
                        var anti = ctx.RequestServices.GetRequiredService<IAntiforgery>();
                        var tokens = anti.GetAndStoreTokens(ctx);
                        ctx.Response.Cookies.Append(
                            "X-ANTIFORGERY-TOKEN",
                            tokens.RequestToken ?? string.Empty,
                            new CookieOptions
                            {
                                HttpOnly = false,
                                Secure = true,
                                SameSite = SameSiteMode.Lax
                            });
                    }
                }
                await next();
            });
        }
    }
}
