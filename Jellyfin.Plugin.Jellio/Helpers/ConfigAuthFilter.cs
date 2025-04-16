using System.Threading.Tasks;
using Jellyfin.Plugin.Jellio.Models;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Jellyfin.Plugin.Jellio.Helpers;

public class ConfigAuthFilter(IUserManager userManager, IDeviceManager deviceManager)
    : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next
    )
    {
        if (context.ActionArguments.TryGetValue("config", out var cfg) && cfg is ConfigModel config)
        {
            var user = RequestHelpers.GetUserByAuthToken(
                config.AuthToken,
                userManager,
                deviceManager
            );
            if (user == null)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            context.HttpContext.Items["JellioUser"] = user;
        }

        await next().ConfigureAwait(false);
    }
}
