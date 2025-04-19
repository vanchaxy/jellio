using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Jellio.Models;

public class ConfigModel
{
    public required string AuthToken { get; init; }

    public required IReadOnlyList<Guid> LibrariesGuids { get; init; }
}
