using System;
using System.Collections.Generic;
using System.Dynamic;
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

    private object MapToMeta(
        BaseItemDto dto,
        StremioType stremioType,
        string baseUrl,
        bool includeDetails = false
    )
    {
        dynamic meta = new ExpandoObject();

        meta.id = dto.ProviderIds.TryGetValue("Imdb", out var imdbId) ? imdbId : $"jellio:{dto.Id}";
        meta.type = stremioType.ToString().ToLower(CultureInfo.InvariantCulture);
        meta.name = dto.Name;
        meta.poster = baseUrl + "/Items/" + dto.Id + "/Images/Primary";
        meta.posterShape = "poster";
        meta.genres = dto.Genres;

        if (dto.Overview != null)
        {
            meta.description = dto.Overview;
        }

        if (dto.CommunityRating.HasValue)
        {
            meta.imdbRating = dto.CommunityRating.Value.ToString(
                "F1",
                CultureInfo.InvariantCulture
            );
        }

        if (dto.PremiereDate.HasValue)
        {
            var premiereYear = dto.PremiereDate.Value.Year.ToString(CultureInfo.InvariantCulture);
            meta.releaseInfo = premiereYear;

            if (stremioType is StremioType.Series)
            {
                meta.releaseInfo += "-";
                if (dto.Status != "Continuing" && dto.EndDate.HasValue)
                {
                    var endYear = dto.EndDate.Value.Year.ToString(CultureInfo.InvariantCulture);
                    if (premiereYear != endYear)
                    {
                        meta.releaseInfo += dto.EndDate.Value.Year.ToString(
                            CultureInfo.InvariantCulture
                        );
                    }
                }
            }
        }

        if (includeDetails)
        {
            if (dto.RunTimeTicks.HasValue && dto.RunTimeTicks.Value != 0)
            {
                var runTimeMin = dto.RunTimeTicks.Value / 600000000;
                meta.runtime = runTimeMin.ToString(CultureInfo.InvariantCulture) + " min";
            }

            if (dto.ImageTags.ContainsKey(ImageType.Logo))
            {
                meta.logo = baseUrl + "/Items/" + dto.Id + "/Images/Logo";
            }

            if (dto.BackdropImageTags.Length != 0)
            {
                meta.background = baseUrl + "/Items/" + dto.Id + "/Images/Backdrop/0";
            }

            if (dto.PremiereDate.HasValue)
            {
                meta.released = dto.PremiereDate.Value.ToString("o");
            }
        }

        return meta;
    }

    private OkObjectResult GetStreamsResult(User user, List<BaseItem> items)
    {
        var baseUrl = GetBaseUrl();
        var dtoOptions = new DtoOptions(true);
        var dtos = dtoService.GetBaseItemDtos(items, dtoOptions, user);
        var streams = dtos.SelectMany(dto =>
            dto.MediaSources.Select(source => new
            {
                url = $"{baseUrl}/videos/{dto.Id}/stream?mediaSourceId={source.Id}&static=true",
                name = "Jellio",
                description = source.Name,
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

        var dtoOptions = new DtoOptions
        {
            Fields = [ItemFields.ProviderIds, ItemFields.Overview, ItemFields.Genres],
        };
        var query = new InternalItemsQuery(user)
        {
            Recursive = false,
            OrderBy =
            [
                (ItemSortBy.ProductionYear, SortOrder.Descending),
                (ItemSortBy.SortName, SortOrder.Ascending),
            ],
            Limit = 100,
            StartIndex = 0,
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
        dynamic meta = MapToMeta(dto, stremioType, baseUrl, includeDetails: true);

        if (stremioType is StremioType.Series)
        {
            if (item is not Series series)
            {
                return BadRequest();
            }

            var episodes = series.GetEpisodes(user, dtoOptions, false).ToList();
            var seriesItemOptions = new DtoOptions { Fields = [ItemFields.Overview] };
            var dtos = dtoService.GetBaseItemDtos(episodes, seriesItemOptions, user);
            var videos = dtos.Select(episode =>
            {
                dynamic video = new ExpandoObject();
                video.id = $"jellio:{episode.Id}";
                video.title = episode.Name;
                video.thumbnail = baseUrl + "/Items/" + episode.Id + "/Images/Primary";
                video.available = true;
                video.episode = episode.IndexNumber;
                video.season = episode.ParentIndexNumber;
                video.overview = episode.Overview;
                if (episode.PremiereDate.HasValue)
                {
                    video.released = episode.PremiereDate.Value.ToString("o");
                }

                return video;
            });
            meta.videos = videos;
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
            HasAnyProviderId = new Dictionary<string, string> { ["Imdb"] = "tt" + imdbId },
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
            HasAnyProviderId = new Dictionary<string, string> { ["Imdb"] = "tt" + imdbId },
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
