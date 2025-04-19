using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mime;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Jellio.Helpers;
using Jellyfin.Plugin.Jellio.Models;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Jellio.Controllers;

[ApiController]
[ConfigAuthorize]
[Route("jellio/{config}")]
[Produces(MediaTypeNames.Application.Json)]
public class AddonController(
    IUserViewManager userViewManager,
    IDtoService dtoService,
    ILibraryManager libraryManager
) : ControllerBase
{
    private string GetBaseUrl()
    {
        return $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
    }

    private static MetaDto MapToMeta(
        BaseItemDto dto,
        StremioType stremioType,
        string baseUrl,
        bool includeDetails = false
    )
    {
        string? releaseInfo = null;
        if (dto.PremiereDate.HasValue)
        {
            var premiereYear = dto.PremiereDate.Value.Year.ToString(CultureInfo.InvariantCulture);
            releaseInfo = premiereYear;
            if (stremioType == StremioType.Series)
            {
                releaseInfo += "-";
                if (dto.Status != "Continuing" && dto.EndDate.HasValue)
                {
                    var endYear = dto.EndDate.Value.Year.ToString(CultureInfo.InvariantCulture);
                    if (premiereYear != endYear)
                    {
                        releaseInfo += endYear;
                    }
                }
            }
        }

        var meta = new MetaDto
        {
            Id = dto.ProviderIds.TryGetValue("Imdb", out var idVal) ? idVal : $"jellio:{dto.Id}",
            Type = stremioType.ToString().ToLower(CultureInfo.InvariantCulture),
            Name = dto.Name,
            Poster = $"{baseUrl}/Items/{dto.Id}/Images/Primary",
            PosterShape = "poster",
            Genres = dto.Genres,
            Description = dto.Overview,
            ImdbRating = dto.CommunityRating?.ToString("F1", CultureInfo.InvariantCulture),
            ReleaseInfo = releaseInfo,
        };

        if (includeDetails)
        {
            meta.Runtime =
                dto.RunTimeTicks.HasValue && dto.RunTimeTicks.Value != 0
                    ? $"{dto.RunTimeTicks.Value / 600000000} min"
                    : null;
            meta.Logo = dto.ImageTags.ContainsKey(ImageType.Logo)
                ? $"{baseUrl}/Items/{dto.Id}/Images/Logo"
                : null;
            meta.Background =
                dto.BackdropImageTags.Length != 0
                    ? $"{baseUrl}/Items/{dto.Id}/Images/Backdrop/0"
                    : null;
            meta.Released = dto.PremiereDate?.ToString("o");
        }

        return meta;
    }

    private OkObjectResult GetStreamsResult(User user, List<BaseItem> items)
    {
        var baseUrl = GetBaseUrl();
        var dtoOptions = new DtoOptions(true);
        var dtos = dtoService.GetBaseItemDtos(items, dtoOptions, user);
        var streams = dtos.SelectMany(dto =>
            dto.MediaSources.Select(source => new StreamDto
            {
                Url = $"{baseUrl}/videos/{dto.Id}/stream?mediaSourceId={source.Id}&static=true",
                Name = "Jellio",
                Description = source.Name,
            })
        );
        return Ok(new { streams });
    }

    [HttpGet("manifest.json")]
    public IActionResult GetManifest([ConfigFromBase64Json] ConfigModel config)
    {
        var user = (User)HttpContext.Items["JellioUser"]!;

        var userLibraries = LibraryHelper.GetUserLibraries(user, userViewManager, dtoService);
        userLibraries = Array.FindAll(userLibraries, l => config.LibrariesGuids.Contains(l.Id));
        if (userLibraries.Length == 0)
        {
            return NotFound();
        }

        var catalogs = userLibraries.Select(lib =>
        {
            return new
            {
                type = lib.CollectionType switch
                {
                    CollectionType.movies => "movie",
                    CollectionType.tvshows => "series",
                    _ => null,
                },
                id = lib.Id.ToString(),
                name = lib.Name,
                extra = new[]
                {
                    new { name = "skip", isRequired = false },
                    new { name = "search", isRequired = false },
                },
            };
        });

        var manifest = new
        {
            id = "com.stremio.jellio",
            version = "0.0.1",
            name = "Jellio",
            description = "Play movies and series from Jellyfin.",
            resources = new object[]
            {
                "catalog",
                "stream",
                new
                {
                    name = "meta",
                    types = new[] { "movie", "series" },
                    idPrefixes = new[] { "jellio" },
                },
            },
            types = new[] { "movie", "series" },
            idPrefixes = new[] { "tt", "jellio" },
            contactEmail = "support@jellio.stream",
            behaviorHints = new { configurable = true },
            catalogs,
        };

        return Ok(manifest);
    }

    [HttpGet("catalog/{stremioType}/{catalogId:guid}/{extra}.json")]
    [HttpGet("catalog/{stremioType}/{catalogId:guid}.json")]
    public IActionResult GetCatalog(
        [ConfigFromBase64Json] ConfigModel config,
        StremioType stremioType,
        Guid catalogId,
        string? extra = null
    )
    {
        var user = (User)HttpContext.Items["JellioUser"]!;

        var userLibraries = LibraryHelper.GetUserLibraries(user, userViewManager, dtoService);
        var catalogLibrary = Array.Find(userLibraries, l => l.Id == catalogId);
        if (catalogLibrary == null)
        {
            return NotFound();
        }

        var item = libraryManager.GetParentItem(catalogLibrary.Id, user.Id);
        if (item is not Folder folder)
        {
            folder = libraryManager.GetUserRootFolder();
        }

        var extras =
            extra
                ?.Split('&')
                .Select(e => e.Split('='))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => parts[1])
            ?? new Dictionary<string, string>();

        int startIndex =
            extras.TryGetValue("skip", out var skipValue)
            && int.TryParse(skipValue, out var parsedSkip)
                ? parsedSkip
                : 0;
        extras.TryGetValue("search", out var searchTerm);

        var dtoOptions = new DtoOptions
        {
            Fields = [ItemFields.ProviderIds, ItemFields.Overview, ItemFields.Genres],
        };
        var query = new InternalItemsQuery(user)
        {
            Recursive = true, // need this for search to work
            IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.Series],
            OrderBy =
            [
                (ItemSortBy.ProductionYear, SortOrder.Descending),
                (ItemSortBy.SortName, SortOrder.Ascending),
            ],
            Limit = 100,
            StartIndex = startIndex,
            SearchTerm = searchTerm,
            ParentId = catalogLibrary.Id,
            DtoOptions = dtoOptions,
        };
        var result = folder.GetItems(query);
        var dtos = dtoService.GetBaseItemDtos(result.Items, dtoOptions, user);
        var baseUrl = GetBaseUrl();
        var metas = dtos.Select(dto => MapToMeta(dto, stremioType, baseUrl));

        return Ok(new { metas });
    }

    [HttpGet("meta/{stremioType}/jellio:{mediaId:guid}.json")]
    public IActionResult GetMeta(
        [ConfigFromBase64Json] ConfigModel config,
        StremioType stremioType,
        Guid mediaId
    )
    {
        var user = (User)HttpContext.Items["JellioUser"]!;

        var item = libraryManager.GetItemById<BaseItem>(mediaId, user);
        if (item == null)
        {
            return NotFound();
        }

        var dtoOptions = new DtoOptions
        {
            Fields = [ItemFields.ProviderIds, ItemFields.Overview, ItemFields.Genres],
        };
        var dto = dtoService.GetBaseItemDto(item, dtoOptions, user);
        var baseUrl = GetBaseUrl();
        var meta = MapToMeta(dto, stremioType, baseUrl, includeDetails: true);

        if (stremioType is StremioType.Series)
        {
            if (item is not Series series)
            {
                return BadRequest();
            }

            var episodes = series.GetEpisodes(user, dtoOptions, false).ToList();
            var seriesItemOptions = new DtoOptions { Fields = [ItemFields.Overview] };
            var dtos = dtoService.GetBaseItemDtos(episodes, seriesItemOptions, user);
            var videos = dtos.Select(episode => new VideoDto
            {
                Id = $"jellio:{episode.Id}",
                Title = episode.Name,
                Thumbnail = $"{baseUrl}/Items/{episode.Id}/Images/Primary",
                Available = true,
                Episode = episode.IndexNumber ?? 0,
                Season = episode.ParentIndexNumber ?? 0,
                Overview = episode.Overview,
                Released = episode.PremiereDate?.ToString("o"),
            });
            meta.Videos = videos;
        }

        return Ok(new { meta });
    }

    [HttpGet("stream/{stremioType}/jellio:{mediaId:guid}.json")]
    public IActionResult GetStream(
        [ConfigFromBase64Json] ConfigModel config,
        StremioType stremioType,
        Guid mediaId
    )
    {
        var user = (User)HttpContext.Items["JellioUser"]!;

        var item = libraryManager.GetItemById<BaseItem>(mediaId, user);
        if (item == null)
        {
            return NotFound();
        }

        return GetStreamsResult(user, [item]);
    }

    [HttpGet("stream/movie/tt{imdbId}.json")]
    public IActionResult GetStreamImdbMovie(
        [ConfigFromBase64Json] ConfigModel config,
        string imdbId
    )
    {
        var user = (User)HttpContext.Items["JellioUser"]!;

        var query = new InternalItemsQuery(user)
        {
            HasAnyProviderId = new Dictionary<string, string> { ["Imdb"] = $"tt{imdbId}" },
            IncludeItemTypes = [BaseItemKind.Movie],
        };
        var items = libraryManager.GetItemList(query);

        return GetStreamsResult(user, items);
    }

    [HttpGet("stream/series/tt{imdbId}:{seasonNum:int}:{episodeNum:int}.json")]
    public IActionResult GetStreamImdbTv(
        [ConfigFromBase64Json] ConfigModel config,
        string imdbId,
        int seasonNum,
        int episodeNum
    )
    {
        var user = (User)HttpContext.Items["JellioUser"]!;

        var seriesQuery = new InternalItemsQuery(user)
        {
            IncludeItemTypes = [BaseItemKind.Series],
            HasAnyProviderId = new Dictionary<string, string> { ["Imdb"] = $"tt{imdbId}" },
        };
        var seriesItems = libraryManager.GetItemList(seriesQuery);

        if (seriesItems.Count == 0)
        {
            return NotFound();
        }

        var seriesIds = seriesItems.Select(s => s.Id).ToArray();

        var episodeQuery = new InternalItemsQuery(user)
        {
            IncludeItemTypes = [BaseItemKind.Episode],
            AncestorIds = seriesIds,
            ParentIndexNumber = seasonNum,
            IndexNumber = episodeNum,
        };
        var episodeItems = libraryManager.GetItemList(episodeQuery);

        return GetStreamsResult(user, episodeItems);
    }
}
