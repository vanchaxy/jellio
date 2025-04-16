using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Jellio.Helpers;

public sealed class ConfigFromBase64Json() : ModelBinderAttribute(typeof(Base64JsonModelBinder));
