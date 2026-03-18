using Codeer.LowCode.Blazor.RequestInterfaces;

namespace Plesanter.CLB.Client.Shared.Services
{
    public static class ServicesExtensions
    {
        public static IAppInfoServiceExtension AsImplement(this IAppInfoService src) => (IAppInfoServiceExtension)src;
    }
}
