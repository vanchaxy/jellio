using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Jellio.Helpers;

public sealed class ConfigAuthorizeAttribute() : TypeFilterAttribute(typeof(ConfigAuthFilter));
