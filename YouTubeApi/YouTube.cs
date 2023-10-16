using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace YouTubeAPi;

/// <summary>
/// Class to search for and retrieve playlist and video data from YouTube using the web client API.
/// </summary>
public static partial class YouTube
{
    /// <summary>
    /// Set the content type filter preference for YouTube Music searches.
    /// </summary>
    public enum MusicSearchFilter
    {
        /// <summary>
        /// Filters YouTube Music search results to albums.
        /// </summary>
        Albums,

        /// <summary>
        /// Filters YouTube Music search results to featured playlists.
        /// </summary>
        FeaturedPlaylists,

        /// <summary>
        /// Filters YouTube Music search results to community playlists.
        /// </summary>
        CommunityPlaylists,

        /// <summary>
        /// Filters YouTube Music search results to songs.
        /// </summary>
        Songs,
    }

    /// <summary>
    /// Returns content by section from the YouTube music channel home page.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to stop the content download if connectivity is lost during the process.</param>
    /// <returns><see cref="HomePageSection"/>s from the YouTube music channel page.</returns>
    public static async Task<List<HomePageSection>> GetHomePageContentSections(
        CancellationToken? cancellationToken = null
    )
    {
        var homepageSections = new List<HomePageSection>();
        var url =
            Constants.YouTubeUrl
            + Constants.BaseApi
            + Constants.BrowseEndpoint
            + Constants.ApiParameters;
        var data = new Dictionary<string, object>
        {
            ["browseId"] = Constants.YouTubeMusicChannelBrowseId,
        };
        var jsonData = JObject.FromObject(data);

        var response = await Network.SendPostRequest(url, jsonData, null, cancellationToken);
        var jsonResponse = JToken.Parse(response);
        var shelfRenderers = JsonParsers.ExtractShelfRenderers(jsonResponse);

        foreach (var shelfRenderer in shelfRenderers)
        {
            // Add a section header home item based on the text in the shelf renderer
            var sectionHeader = (string?)shelfRenderer.SelectTokens("title.runs[*].text").First();

            if (sectionHeader == null)
                continue;

            var homepageSection = new HomePageSection(sectionHeader);

            // Create a list of possible item types that may be stored within a shelf renderer
            var itemTypes = new string[]
            {
                "compactStationRenderer",
                "gridVideoRenderer",
                "gridPlaylistRenderer"
            };

            List<JToken> jsonHomeItems = new List<JToken>();
            string? itemType = null;
            var i = 0;

            // Attempt to extract possible content item types from the shelf renderer and save the type name if data was found
            while (jsonHomeItems.Count == 0)
            {
                jsonHomeItems = shelfRenderer.SelectTokens("$.." + itemTypes[i]).ToList();
                itemType = itemTypes[i];
                i++;
            }

            foreach (var jsonHomeItem in jsonHomeItems)
            {
                if (itemType == "compactStationRenderer")
                    homepageSection.ContentItems.Add(
                        new ContentItem(
                            JsonParsers.ExtractPlaylistFromCompactStationRenderer(jsonHomeItem)
                        )
                    );
                else if (itemType == "gridVideoRenderer")
                    homepageSection.ContentItems.Add(
                        new ContentItem(JsonParsers.ExtractVideoFromGridVideoRenderer(jsonHomeItem))
                    );
                else if (itemType == "gridPlaylistRenderer")
                    homepageSection.ContentItems.Add(
                        new ContentItem(
                            JsonParsers.ExtractPlaylistFromGridPlaylistRenderer(jsonHomeItem)
                        )
                    );
            }

            if (homepageSection.ContentItems.Count > 0)
                homepageSections.Add(homepageSection);
        }

        return homepageSections;
    }

    /// <summary>
    /// Retrieves video and audio stream info for a specified YouTube video URL.
    /// </summary>
    /// <param name="videoId">The video ID to get related videos from.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <returns>A list of <see cref="StreamInfo"/>s for the video.</returns>
    public static async Task<List<StreamInfo>> GetStreamInfo(
        string videoId,
        CancellationToken? cancellationToken = null
    )
    {
        var streamInfoItems = new List<StreamInfo>();
        var url =
            Constants.YouTubeUrl
            + Constants.BaseApi
            + Constants.PlayerEndpoint
            + Constants.ApiParameters;
        var data = new Dictionary<string, object> { ["videoId"] = videoId, };
        var jsonData = JObject.FromObject(data);

        var response = await Network.SendPostRequest(url, jsonData, null, cancellationToken);
        var jsonResponse = JToken.Parse(response);
        var jsonStreamData = JsonParsers.ExtractStreamData(jsonResponse);

        if (jsonStreamData == null)
            return streamInfoItems;

        foreach (var jsonStreamDataItem in jsonStreamData)
        {
            var streamUrl = (string?)jsonStreamDataItem["url"];
            var mimeType = (string?)jsonStreamDataItem["mimeType"];
            var bitRate = (int?)jsonStreamDataItem["bitrate"];

            if (streamUrl != null && mimeType != null && bitRate != null)
                streamInfoItems.Add(new StreamInfo(streamUrl, mimeType, (int)bitRate));
        }

        return streamInfoItems;
    }

    /// <summary>
    /// Searches YouTube and returns matching results.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <returns>A <see cref="PaginatedResults"/> set matching the search query.</returns>
    public static async Task<PaginatedResults> SearchYouTube(
        string query,
        CancellationToken? cancellationToken = null
    )
    {
        async Task<JToken?> SendPageRequest(
            string? continuationToken,
            string? visitorData,
            CancellationToken? cancellationToken
        )
        {
            var url =
                Constants.YouTubeUrl
                + Constants.BaseApi
                + Constants.SearchEndpoint
                + Constants.ApiParameters;
            var data = new Dictionary<string, object?>
            {
                ["query"] = query,
                ["continuation"] = continuationToken,
            };
            var jsonData = JObject.FromObject(data);

            var response = await Network.SendPostRequest(url, jsonData, null, cancellationToken);

            return JToken.Parse(response);
        }

        Page ParsePageContentItems(JToken? jsonResponse, int pageNumber)
        {
            var page = new Page(pageNumber);

            if (jsonResponse == null)
                return page;

            var searchResultJsonItems = JsonParsers.ExtractContentsFromSearchResults(jsonResponse);

            if (searchResultJsonItems == null)
                return page;

            foreach (var searchResultJsonItem in searchResultJsonItems)
            {
                if (searchResultJsonItem["videoRenderer"] != null)
                {
                    var videoRenderer = searchResultJsonItem["videoRenderer"];
                    var video = JsonParsers.ExtractVideoFromVideoRenderer(videoRenderer);

                    if (video != null)
                        page.ContentItems.Add(new ContentItem(video));
                }
                else if (searchResultJsonItem["playlistRenderer"] != null)
                {
                    var playlistRenderer = searchResultJsonItem["playlistRenderer"];
                    var playlist = JsonParsers.ExtractPlaylistFromPlaylistRenderer(
                        playlistRenderer
                    );

                    if (playlist != null)
                        page.ContentItems.Add(new ContentItem(playlist));
                }
            }

            return page;
        }

        var paginatedResults = new PaginatedResults(SendPageRequest, ParsePageContentItems);
        await paginatedResults.GetNextPage(cancellationToken);

        return paginatedResults;
    }

    /// <summary>
    /// Performs YouTube Music search filtered by content category.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="searchFilter">A <see cref="MusicSearchFilter"/> to set the desired search content category.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <returns>A <see cref="PaginatedResults"/> set matching the search query and filter.</returns>
    public static async Task<PaginatedResults> SearchYouTubeMusic(
        string query,
        MusicSearchFilter searchFilter,
        CancellationToken? cancellationToken = null
    )
    {
        async Task<JToken?> SendPageRequest(
            string? continuationToken,
            string? visitorData,
            CancellationToken? cancellationToken
        )
        {
            var url =
                Constants.YouTubeMusicUrl
                + Constants.BaseApi
                + Constants.SearchEndpoint
                + Constants.ApiParameters;
            var data = new Dictionary<string, object?>
            {
                ["query"] = query,
                ["continuation"] = continuationToken,
            };

            string? parameters;

            if (searchFilter == MusicSearchFilter.Albums)
                parameters = "EgWKAQIYAWoMEA4QChADEAQQCRAF";
            else if (searchFilter == MusicSearchFilter.FeaturedPlaylists)
                parameters = "EgeKAQQoADgBagwQDhAKEAMQBBAJEAU%3D";
            else if (searchFilter == MusicSearchFilter.CommunityPlaylists)
                parameters = "EgeKAQQoAEABagwQDhAKEAMQBBAJEAU%3D";
            else
                // MusicSearchFilter.Songs
                parameters = "EgWKAQIIAWoMEA4QChADEAQQCRAF";

            data["params"] = parameters;
            var jsonData = JObject.FromObject(data);

            var response = await Network.SendPostRequest(url, jsonData, null, cancellationToken);

            return JToken.Parse(response);
        }

        Page ParsePageContentItems(JToken? jsonResponse, int pageNumber)
        {
            var page = new Page(pageNumber);

            if (jsonResponse == null)
                return page;

            var musicResponsiveListItemRenderers =
                JsonParsers.ExtractMusicResponsiveListItemRenderers(jsonResponse);

            foreach (var musicResponsiveListItemRenderer in musicResponsiveListItemRenderers)
            {
                if (
                    searchFilter == MusicSearchFilter.Albums
                    || searchFilter == MusicSearchFilter.FeaturedPlaylists
                    || searchFilter == MusicSearchFilter.CommunityPlaylists
                )
                {
                    var playlist = JsonParsers.ExtractPlaylistFromMusicResponsiveListItemRenderer(
                        musicResponsiveListItemRenderer
                    );

                    if (playlist != null)
                        page.ContentItems.Add(new ContentItem(playlist));
                }
                else if (searchFilter == MusicSearchFilter.Songs)
                {
                    var video = JsonParsers.ExtractVideoFromMusicResponsiveListItemRenderer(
                        musicResponsiveListItemRenderer
                    );

                    if (video != null)
                        page.ContentItems.Add(new ContentItem(video));
                }
            }

            return page;
        }

        var paginatedResults = new PaginatedResults(SendPageRequest, ParsePageContentItems);
        await paginatedResults.GetNextPage(cancellationToken);

        return paginatedResults;
    }

    /// <summary>
    /// Retrieves related videos for a specific input video ID.
    /// </summary>
    /// <param name="videoId">A video ID value.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <returns>A <see cref="PaginatedResults"/> set of related videos.</returns>
    public static async Task<PaginatedResults> GetRelatedVideos(
        string videoId,
        CancellationToken? cancellationToken = null
    )
    {
        async Task<JToken?> SendPageRequest(
            string? continuationToken,
            string? visitorData,
            CancellationToken? cancellationToken
        )
        {
            var url =
                Constants.YouTubeUrl
                + Constants.BaseApi
                + Constants.NextEndpoint
                + Constants.ApiParameters;
            var data = new Dictionary<string, object?>
            {
                ["videoId"] = videoId,
                ["continuation"] = continuationToken,
                ["context"] = new Dictionary<string, object>
                {
                    ["client"] = new Dictionary<string, object?> { ["visitorData"] = visitorData, },
                },
            };
            var jsonData = JObject.FromObject(data);

            var response = await Network.SendPostRequest(url, jsonData, null, cancellationToken);

            return JToken.Parse(response);
        }

        Page ParsePageContentItems(JToken? jsonResponse, int pageNumber)
        {
            var page = new Page(pageNumber);

            if (jsonResponse == null)
                return page;

            var compactVideoRenderers = JsonParsers.ExtractCompactVideoRenderers(jsonResponse);

            foreach (var compactVideoRenderer in compactVideoRenderers)
            {
                var video = JsonParsers.ExtractVideoFromCompactVideoRenderer(compactVideoRenderer);

                if (video != null)
                    page.ContentItems.Add(new ContentItem(video));
            }

            return page;
        }

        var paginatedResults = new PaginatedResults(SendPageRequest, ParsePageContentItems);
        await paginatedResults.GetNextPage(cancellationToken);

        return paginatedResults;
    }

    /// <summary>
    /// Returns videos for a specific playlist.
    /// </summary>
    /// <param name="playlistId">The playlist ID value.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <returns>A <see cref="PaginatedResults"/> set of videos in the playlist.</returns>
    public static async Task<PaginatedResults> GetPlaylistVideos(
        string playlistId,
        CancellationToken? cancellationToken = null
    )
    {
        async Task<JToken?> SendPageRequest(
            string? continuationToken,
            string? visitorData,
            CancellationToken? cancellationToken
        )
        {
            var url =
                Constants.YouTubeUrl
                + Constants.BaseApi
                + Constants.BrowseEndpoint
                + Constants.ApiParameters;
            var data = new Dictionary<string, object?>
            {
                ["browseId"] = "VL" + playlistId,
                ["continuation"] = continuationToken,
                ["context"] = new Dictionary<string, object>
                {
                    ["client"] = new Dictionary<string, object?> { ["visitorData"] = visitorData, },
                },
            };
            var jsonData = JObject.FromObject(data);

            var response = await Network.SendPostRequest(url, jsonData, null, cancellationToken);

            return JToken.Parse(response);
        }

        Page ParsePageContentItems(JToken? jsonResponse, int pageNumber)
        {
            var page = new Page(pageNumber);

            if (jsonResponse == null)
                return page;

            var playlistVideoRenderers = JsonParsers.ExtractPlaylistVideoRenderers(jsonResponse);

            foreach (var playlistVideoRenderer in playlistVideoRenderers)
            {
                var video = JsonParsers.ExtractVideoFromVideoRenderer(playlistVideoRenderer);

                if (video != null)
                    page.ContentItems.Add(new ContentItem(video));
            }

            return page;
        }

        var paginatedResults = new PaginatedResults(SendPageRequest, ParsePageContentItems);
        await paginatedResults.GetNextPage(cancellationToken);

        return paginatedResults;
    }

    /// <summary>
    /// Returns a generated playlist of songs similar to the input video.
    /// </summary>
    /// <param name="videoId">The video ID value.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <returns>A <see cref="PaginatedResults"/> of videos in the generated playlist.</returns>
    public static async Task<PaginatedResults> GetWatchPlaylistVideos(
        string videoId,
        CancellationToken? cancellationToken = null
    )
    {
        async Task<JToken?> SendPageRequest(
            string? continuationToken,
            string? visitorData,
            CancellationToken? cancellationToken
        )
        {
            var url =
                Constants.YouTubeMusicUrl
                + Constants.BaseApi
                + Constants.NextEndpoint
                + Constants.ApiParameters;
            var data = new Dictionary<string, object?>
            {
                ["enablePersistentPlaylistPanel"] = true,
                ["isAudioOnly"] = true,
                ["videoId"] = videoId,
                ["playlistId"] = "RDAMVM" + videoId,
                ["continuation"] = continuationToken,
                ["watchEndpointMusicSupportedConfigs"] = new Dictionary<string, object>
                {
                    ["watchEndpointMusicConfig"] = new Dictionary<string, object>
                    {
                        ["hasPersistentPlaylistPanel"] = true,
                        ["musicVideoType"] = "MUSIC_VIDEO_TYPE_OMV",
                    },
                },
            };
            var jsonData = JObject.FromObject(data);

            var response = await Network.SendPostRequest(
                url,
                jsonData,
                visitorData,
                cancellationToken
            );

            return JToken.Parse(response);
        }

        Page ParsePageContentItems(JToken? jsonResponse, int pageNumber)
        {
            var page = new Page(pageNumber);

            if (jsonResponse == null)
                return page;

            var playlistPanelVideoRenderers = JsonParsers.ExtractPlaylistPanelVideoRenderer(
                jsonResponse
            );

            foreach (var playlistPanelVideoRenderer in playlistPanelVideoRenderers)
            {
                var video = JsonParsers.ExtractVideoFromPlaylistPanelVideoRenderer(
                    playlistPanelVideoRenderer
                );

                if (video != null)
                    page.ContentItems.Add(new ContentItem(video));
            }

            return page;
        }

        var paginatedResults = new PaginatedResults(SendPageRequest, ParsePageContentItems);
        await paginatedResults.GetNextPage(cancellationToken);

        return paginatedResults;
    }

    /// <summary>
    /// Extracts the YouTube video ID value from a URL.
    /// </summary>
    /// <param name="url">A YouTube or YouTube Music video URL.</param>
    /// <returns>The video ID value.</returns>
    public static string GetVideoId(string url)
    {
        var videoIdRegex = new Regex(
            @"[&?]v=([^&]*)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );
        var videoIdMatch = videoIdRegex.Match(url);
        var videoId = videoIdMatch.Groups[1].Value;

        return videoId;
    }

    /// <summary>
    /// Extracts a YouTube playlist ID value from a URL.
    /// </summary>
    /// <param name="url">A YouTube playlist URL.</param>
    /// <returns>The playlist ID.</returns>
    public static string GetPlaylistId(string url)
    {
        var playlistIdRegex = new Regex(
            @"[&?]list=([^&]*)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );
        var playlistIdMatch = playlistIdRegex.Match(url);
        var playlistId = playlistIdMatch.Groups[1].Value;

        return playlistId;
    }
}
