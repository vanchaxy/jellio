using System;
using System.Security.Claims;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Queries;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.Jellio.Helpers;

public static class RequestHelpers
{
    internal static Guid? GetCurrentUserId(ClaimsPrincipal claimsPrincipal)
    {
        var userIdString = claimsPrincipal.FindFirstValue("Jellyfin-UserId");

        if (string.IsNullOrEmpty(userIdString))
        {
            return null;
        }

        if (!Guid.TryParse(userIdString, out var userIdGuid))
        {
            return null;
        }

        return userIdGuid;
    }

    internal static User? GetCurrentUser(ClaimsPrincipal claimsPrincipal, IUserManager userManager)
    {
        var userIdString = claimsPrincipal.FindFirstValue("Jellyfin-UserId");

        if (string.IsNullOrEmpty(userIdString))
        {
            return null;
        }

        return !Guid.TryParse(userIdString, out var userIdGuid)
            ? null
            : userManager.GetUserById(userIdGuid);
    }

    internal static User? GetUserByAuthToken(
        string authToken,
        IUserManager userManager,
        IDeviceManager deviceManager
    )
    {
        var items = deviceManager
            .GetDevices(new DeviceQuery { AccessToken = authToken, Limit = 1 })
            .Items;

        return items.Count == 0 ? null : userManager.GetUserById(items[0].UserId);
    }
}
