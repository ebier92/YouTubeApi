using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace YouTubeApi;

/// <summary>
/// Class to search for and retrieve playlist and video data from YouTube using the web client API.
/// </summary>
public static partial class YouTube
{
    /// <summary>
    /// Class to handle parsing methods to extract content from JSON objects.
    /// </summary>
    private static class JsonParsers
    {
        /// <summary>
        /// Extracts the continuation token to get the next page of search results from the YouTube search API.
        /// </summary>
        /// <param name="json">YouTube JSON.</param>
        /// <returns>The API continuation token.</returns>
        public static string? ExtractContinuationToken(JToken json)
        {
            string? continuationToken = null;

            try
            {
                var continuationTokens = json.SelectTokens("$..token");
                // Use the longest token found if multiple tokens exist
                continuationToken = continuationTokens
                    .OrderByDescending(x => x.ToString().Length)
                    .First()
                    .ToString();
            }
            catch
            {
                try
                {
                    var continuationTokens = json.SelectTokens("$..continuation");
                    // Use the longest token found if multiple tokens exist
                    continuationToken = continuationTokens
                        .OrderByDescending(x => x.ToString().Length)
                        .First()
                        .ToString();
                }
                catch
                {
                    return null;
                }
            }

            return continuationToken;
        }

        /// <summary>
        /// Extracts the visitor data required to get the next page of search results from the YouTube search API.
        /// </summary>
        /// <param name="json">YouTube JSON.</param>
        /// <returns>YouTube visitor data.</returns>
        public static string? ExtractVisitorData(JToken? json) =>
            (string?)json?["responseContext"]?["visitorData"];

        /// <summary>
        /// Extracts the title of a playlist.
        /// </summary>
        /// <param name="json">YouTube JSON.</param>
        /// <returns>The title of the playlist.</returns>
        public static string? ExtractPlaylistTitle(JToken json) =>
            (string?)json["metadata"]?["playlistMetadataRenderer"]?["title"];

        /// <summary>
        /// Extracts audio and video stream info from JSON.
        /// </summary>
        /// <param name="json">JSON returned by YouTube.</param>
        /// <returns>Stream info items as a list of <see cref="JToken"/>s.</returns>
        public static JToken? ExtractStreamData(JToken json)
        {
            var formats = (JArray?)json["streamingData"]?["formats"];
            var adaptiveFormats = (JArray?)json["streamingData"]?["adaptiveFormats"];

            if (formats != null && adaptiveFormats != null)
                formats.Merge(adaptiveFormats);

            return formats;
        }

        /// <summary>
        /// Extracts the search result items from JSON.
        /// </summary>
        /// <param name="json">JSON returned by YouTube.</param>
        /// <returns>Search results as a <see cref="JToken"/>.</returns>
        public static JToken? ExtractContentsFromSearchResults(JToken json)
        {
            JToken? resultItems;

            var itemSectionRenderers = json.SelectTokens("$..itemSectionRenderer").ToList();

            // If there are multiple item section renderers, use the one with the most content
            if (itemSectionRenderers != null && itemSectionRenderers.Count > 1)
            {
                // Initialize with the items from the first item section renderer
                resultItems = itemSectionRenderers[0]["contents"];

                foreach (var itemSectionRenderer in itemSectionRenderers)
                {
                    if (itemSectionRenderer?["contents"]?.Count() > resultItems?.Count())
                        resultItems = itemSectionRenderer["contents"];
                }
            } // Get first and only item section renderer if there is only one
            else if (itemSectionRenderers != null && itemSectionRenderers.Count == 1)
            {
                resultItems = itemSectionRenderers[0]["contents"];
            }
            else
            {
                return null;
            }

            return resultItems;
        }

        /// <summary>
        /// Extracts shelf renderers from JSON.
        /// </summary>
        /// <param name="json">JSON returned by YouTube.</param>
        /// <returns>Shelf renderers as a list of <see cref="JToken"/>s.</returns>
        public static List<JToken> ExtractShelfRenderers(JToken json) =>
            json.SelectTokens("$..shelfRenderer").ToList();

        /// <summary>
        /// Extracts compact video renderers from JSON.
        /// </summary>
        /// <param name="json">JSON returned by YouTube.</param>
        /// <returns>Compact video renderers as a list of <see cref="JToken"/>s.</returns>
        public static List<JToken> ExtractCompactVideoRenderers(JToken json) =>
            json.SelectTokens("$..compactVideoRenderer").ToList();

        /// <summary>
        /// Extracts playlist video renderers from JSON.
        /// </summary>
        /// <param name="json">JSON returned by YouTube.</param>
        /// <returns>Playlist video renderers as a list of <see cref="JToken"/>s.</returns>
        public static List<JToken> ExtractPlaylistVideoRenderers(JToken json) =>
            json.SelectTokens("$..playlistVideoRenderer").ToList();

        /// <summary>
        /// Extracts music responsive list item renderers from JSON.
        /// </summary>
        /// <param name="json">JSON returned by YouTube.</param>
        /// <returns>Music responsive list item renderers as a list of <see cref="JToken"/>s.</returns>
        public static List<JToken> ExtractMusicResponsiveListItemRenderers(JToken json) =>
            json.SelectTokens("$..musicResponsiveListItemRenderer").ToList();

        /// <summary>
        /// Extracts playlist panel video renderers from JSON.
        /// </summary>
        /// <param name="json">JSON returned by YouTube.</param>
        /// <returns>Playlist panel video renderers as a list of <see cref="JToken"/>s.</returns>
        public static List<JToken> ExtractPlaylistPanelVideoRenderer(JToken json) =>
            json.SelectTokens("$..playlistPanelVideoRenderer").ToList();

        /// <summary>
        /// Extracts a <see cref="Video"/> from a grid video renderer JSON object.
        /// </summary>
        /// <param name="gridVideoRenderer">The grid video renderer JSON object.</param>
        /// <returns>A <see cref="Video"/>.</returns>
        public static Video? ExtractVideoFromGridVideoRenderer(JToken gridVideoRenderer)
        {
            // Declare data variables used to create the video
            string? videoId;
            string? title;
            string? author;
            TimeSpan duration;

            try
            {
                videoId = (string?)gridVideoRenderer["videoId"];
                title = (string?)gridVideoRenderer["title"]?["simpleText"];
                var durationString = (string?)
                    gridVideoRenderer
                        .SelectTokens(
                            "thumbnailOverlays[*].thumbnailOverlayTimeStatusRenderer.text.simpleText"
                        )
                        .First();
                duration = ParseDurationString(durationString);
                author = (string?)
                    gridVideoRenderer.SelectTokens("shortBylineText.runs[*].text").First();
            }
            catch
            {
                return null;
            }

            if (
                videoId != null && title != null && author != null && duration.TotalMilliseconds > 0
            )
                return new Video(videoId, title, author, duration);
            else
                return null;
        }

        /// <summary>
        /// Extracts a <see cref="Video"/> from a video renderer JSON object.
        /// </summary>
        /// <param name="videoRenderer">The video renderer JSON object.</param>
        /// <returns>A <see cref="Video"/>.</returns>
        public static Video? ExtractVideoFromVideoRenderer(JToken? videoRenderer)
        {
            string? videoId;
            string? title;
            string? author;
            TimeSpan duration;

            try
            {
                videoId = (string?)videoRenderer?["videoId"];
                title = (string?)videoRenderer?.SelectTokens("title.runs[*].text").First();
                var durationString = (string?)
                    videoRenderer
                        ?.SelectTokens(
                            "thumbnailOverlays[*].thumbnailOverlayTimeStatusRenderer.text.simpleText"
                        )
                        .First();
                duration = ParseDurationString(durationString);
            }
            catch
            {
                return null;
            }

            try
            {
                author = (string?)
                    videoRenderer?.SelectTokens("longBylineText.runs[*].text").First();
            }
            catch
            {
                try
                {
                    // Attempt a different JSON path if the first one causes an error
                    author = (string?)
                        videoRenderer?.SelectTokens("shortBylineText.runs[*].text").First();
                }
                catch
                {
                    author = null;
                }
            }

            if (
                videoId != null && title != null && author != null && duration.TotalMilliseconds > 0
            )
                return new Video(videoId, title, author, duration);
            else
                return null;
        }

        /// <summary>
        /// Extracts a <see cref="Video"/> from a compact video renderer JSON object.
        /// </summary>
        /// <param name="compactVideoRenderer">The compact video renderer JSON object.</param>
        /// <returns>A <see cref="Video"/>.</returns>
        public static Video? ExtractVideoFromCompactVideoRenderer(JToken compactVideoRenderer)
        {
            string? videoId;
            string? title;
            string? author;
            TimeSpan duration;

            try
            {
                videoId = (string?)compactVideoRenderer["videoId"];
                title = (string?)compactVideoRenderer?["title"]?["simpleText"];
                var durationString = (string?)
                    compactVideoRenderer
                        ?.SelectTokens(
                            "thumbnailOverlays[*].thumbnailOverlayTimeStatusRenderer.text.simpleText"
                        )
                        .First();
                duration = ParseDurationString(durationString);
                author = (string?)
                    compactVideoRenderer?.SelectTokens("longBylineText.runs[*].text").First();
            }
            catch
            {
                return null;
            }

            if (
                videoId != null && title != null && author != null && duration.TotalMilliseconds > 0
            )
                return new Video(videoId, title, author, duration);
            else
                return null;
        }

        /// <summary>
        /// Extracts a <see cref="Video"/> from a music responsive list item renderer JSON object.
        /// </summary>
        /// <param name="musicResponsiveListItemRenderer">The music responsive list item renderer JSON object.</param>
        /// <returns>A <see cref="Video"/>.</returns>
        public static Video? ExtractVideoFromMusicResponsiveListItemRenderer(
            JToken musicResponsiveListItemRenderer
        )
        {
            string? videoId;
            string? title;
            string? author;
            string? artist;
            string? album;
            TimeSpan duration;

            try
            {
                videoId = (string?)musicResponsiveListItemRenderer?["playlistItemData"]?["videoId"];
                title = (string?)
                    musicResponsiveListItemRenderer
                        ?["flexColumns"]
                        ?[0]
                        ?["musicResponsiveListItemFlexColumnRenderer"]
                        ?["text"]
                        ?["runs"]
                        ?[0]
                        ?["text"];
                artist = (string?)
                    musicResponsiveListItemRenderer
                        ?["flexColumns"]
                        ?[1]
                        ?["musicResponsiveListItemFlexColumnRenderer"]
                        ?["text"]
                        ?["runs"]
                        ?[0]
                        ?["text"];
                album = (string?)
                    musicResponsiveListItemRenderer
                        ?["flexColumns"]
                        ?[1]
                        ?["musicResponsiveListItemFlexColumnRenderer"]
                        ?["text"]
                        ?["runs"]
                        ?[2]
                        ?["text"];
                var durationString = (string?)
                    musicResponsiveListItemRenderer
                        ?["flexColumns"]
                        ?[1]
                        ?["musicResponsiveListItemFlexColumnRenderer"]
                        ?["text"]
                        ?["runs"]
                        ?[4]
                        ?["text"];
                duration = ParseDurationString(durationString);
            }
            catch
            {
                return null;
            }

            if (artist != null && album != null)
                author = artist + " • " + album;
            else if (artist != null)
                author = artist;
            else
                return null;

            if (
                videoId != null && title != null && author != null && duration.TotalMilliseconds > 0
            )
                return new Video(videoId, title, author, duration);
            else
                return null;
        }

        /// <summary>
        /// Extracts a <see cref="Video"/> from a playlist panel video renderer JSON object.
        /// </summary>
        /// <param name="playlistPanelVideoRenderer">The playlist panel video renderer JSON object.</param>
        /// <returns>A <see cref="Video"/>.</returns>
        public static Video? ExtractVideoFromPlaylistPanelVideoRenderer(
            JToken playlistPanelVideoRenderer
        )
        {
            string? videoId;
            string? title;
            string? author;
            string? artist;
            string? album;
            TimeSpan duration;

            try
            {
                videoId = (string?)playlistPanelVideoRenderer["videoId"];
                title = (string?)playlistPanelVideoRenderer?["title"]?["runs"]?[0]?["text"];
                artist = (string?)
                    playlistPanelVideoRenderer?["longBylineText"]?["runs"]?[0]?["text"];
                album = (string?)
                    playlistPanelVideoRenderer?["longBylineText"]?["runs"]?[2]?["text"];

                if (album?.ToLower()?.Contains("view") ?? false)
                    album = null;
                var durationString = (string?)
                    playlistPanelVideoRenderer?.SelectTokens("lengthText.runs[*].text").First();
                duration = ParseDurationString(durationString);
            }
            catch
            {
                return null;
            }

            if (artist != null && album != null)
                author = artist + " • " + album;
            else if (artist != null)
                author = artist;
            else
                return null;

            if (
                videoId != null && title != null && author != null && duration.TotalMilliseconds > 0
            )
                return new Video(videoId, title, author, duration);
            else
                return null;
        }

        /// <summary>
        /// Extracts a <see cref="Playlist"/> from a compact station renderer JSON object.
        /// </summary>
        /// <param name="compactStationRenderer">The compact station renderer JSON object.</param>
        /// <returns>A <see cref="Playlist"/>.</returns>
        public static Playlist? ExtractPlaylistFromCompactStationRenderer(
            JToken compactStationRenderer
        )
        {
            string? playlistId;
            string? title;
            string? description;
            int videoCount;
            Thumbnails? thumbnails;

            try
            {
                playlistId = (string?)
                    compactStationRenderer
                        ?["navigationEndpoint"]
                        ?["watchPlaylistEndpoint"]
                        ?["playlistId"];
                title = (string?)compactStationRenderer?["title"]?["simpleText"];
                var thumbnailVideoId = (string?)
                    compactStationRenderer
                        ?["navigationEndpoint"]
                        ?["watchPlaylistEndpoint"]
                        ?["videoId"];
                thumbnails = new Thumbnails()
                {
                    LowResUrl = (string?)
                        compactStationRenderer?["thumbnail"]?["thumbnails"]?[0]?["url"],
                    MediumResUrl = (string?)
                        compactStationRenderer?["thumbnail"]?["thumbnails"]?[1]?["url"],
                    HighResUrl = (string?)
                        compactStationRenderer?["thumbnail"]?["thumbnails"]?[2]?["url"],
                    StandardResUrl = (string?)
                        compactStationRenderer?["thumbnail"]?["thumbnails"]?[1]?["url"]
                };
                description = (string?)compactStationRenderer?["description"]?["simpleText"];
                videoCount = int.Parse(
                    (string?)
                        compactStationRenderer?.SelectTokens("videoCountText.runs[*].text")?.First()
                        ?? "0"
                );
            }
            catch
            {
                return null;
            }

            if (
                playlistId != null
                && title != null
                && description != null
                && thumbnails.StandardResUrl != null
            )
                return new Playlist(
                    playlistId,
                    title,
                    author: null,
                    description,
                    videoCount,
                    thumbnails
                );
            else
                return null;
        }

        /// <summary>
        /// Extracts a <see cref="Playlist"/> from a compact station renderer JSON object.
        /// </summary>
        /// <param name="gridPlaylistRenderer">The compact station renderer JSON object.</param>
        /// <returns>A <see cref="Playlist"/>.</returns>
        public static Playlist? ExtractPlaylistFromGridPlaylistRenderer(JToken gridPlaylistRenderer)
        {
            string? playlistId;
            string? title;
            string? author;
            string? description;
            int videoCount;
            Thumbnails? thumbnails;

            try
            {
                playlistId = (string?)gridPlaylistRenderer["playlistId"];
                title = (string?)gridPlaylistRenderer.SelectTokens("title.runs[*].text").First();
                var thumbnailVideoId = (string?)
                    gridPlaylistRenderer?["navigationEndpoint"]?["watchEndpoint"]?["videoId"];
                thumbnails =
                    thumbnailVideoId != null
                        ? new Thumbnails() { VideoId = thumbnailVideoId, }
                        : null;
                description = (string?)
                    gridPlaylistRenderer?.SelectTokens("shortBylineText.runs[*].text").First();
                videoCount = int.Parse(
                    (string?)
                        gridPlaylistRenderer?.SelectTokens("videoCountText.runs[*].text").First()
                        ?? "0"
                );
                author = (string?)
                    gridPlaylistRenderer?.SelectTokens("shortBylineText.runs[*].text").First();
            }
            catch
            {
                return null;
            }

            if (
                playlistId != null
                && title != null
                && author != null
                && description != null
                && videoCount > 0
                && thumbnails?.StandardResUrl != null
            )
                return new Playlist(playlistId, title, author, description, videoCount, thumbnails);
            else
                return null;
        }

        /// <summary>
        /// Extracts a <see cref="Playlist"/> from a playlist renderer JSON object.
        /// </summary>
        /// <param name="playlistRenderer">The video renderer JSON object.</param>
        /// <returns>A <see cref="Playlist"/>.</returns>
        public static Playlist? ExtractPlaylistFromPlaylistRenderer(JToken? playlistRenderer)
        {
            string? playlistId;
            string? title;
            string? author;
            int videoCount;
            Thumbnails? thumbnails;

            try
            {
                playlistId = (string?)playlistRenderer?["playlistId"];
                title = (string?)playlistRenderer?["title"]?["simpleText"];

                var thumbnailUrl =
                    (string?)
                        playlistRenderer?.SelectTokens("thumbnails[*].thumbnails[*].url").First()
                    ?? "";
                var videoIdRegex = new Regex(
                    @"\/vi\/(.+)\/",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase
                );
                var videoIdMatch = videoIdRegex.Match(thumbnailUrl);
                var thumbnailVideoId = videoIdMatch.Groups[1].Value;
                thumbnails = new Thumbnails() { VideoId = thumbnailVideoId, };
                author = (string?)
                    playlistRenderer?.SelectTokens("shortBylineText.runs[*].text").First();
                videoCount = int.Parse((string?)playlistRenderer?["videoCount"] ?? "0");
            }
            catch
            {
                return null;
            }

            if (
                playlistId != null
                && title != null
                && author != null
                && videoCount > 0
                && thumbnails.StandardResUrl != null
            )
                return new Playlist(
                    playlistId,
                    title,
                    author,
                    description: null,
                    videoCount,
                    thumbnails
                );
            else
                return null;
        }

        /// <summary>
        /// Extracts a <see cref="Playlist"/> from a music responsive list renderer JSON object.
        /// </summary>
        /// <param name="musicResponsiveListItemRenderer">The music responsive list renderer JSON object.</param>
        /// <returns>A <see cref="Playlist"/>.</returns>
        public static Playlist? ExtractPlaylistFromMusicResponsiveListItemRenderer(
            JToken musicResponsiveListItemRenderer
        )
        {
            string? playlistId;
            string? title;
            string? author;
            int? videoCount;
            Thumbnails thumbnails;

            try
            {
                playlistId = (string?)
                    musicResponsiveListItemRenderer.SelectTokens("$..playlistId").First();
                title = (string?)
                    musicResponsiveListItemRenderer
                        ?["flexColumns"]
                        ?[0]
                        ?["musicResponsiveListItemFlexColumnRenderer"]
                        ?["text"]
                        ?["runs"]
                        ?[0]
                        ?["text"];
                var thumbnailItems = musicResponsiveListItemRenderer
                    ?["thumbnail"]
                    ?["musicThumbnailRenderer"]
                    ?["thumbnail"]
                    ?["thumbnails"];
                var artist = (string?)
                    musicResponsiveListItemRenderer
                        ?["flexColumns"]
                        ?[1]
                        ?["musicResponsiveListItemFlexColumnRenderer"]
                        ?["text"]
                        ?["runs"]
                        ?[0]
                        ?["text"];
                var textRun = (string?)
                    musicResponsiveListItemRenderer
                        ?["flexColumns"]
                        ?[1]
                        ?["musicResponsiveListItemFlexColumnRenderer"]
                        ?["text"]
                        ?["runs"]
                        ?[2]
                        ?["text"];

                thumbnails = new Thumbnails();

                if (thumbnailItems?.Count() >= 1)
                    thumbnails.LowResUrl = (string?)thumbnailItems[0]?["url"];

                if (thumbnailItems?.Count() >= 2)
                    thumbnails.MediumResUrl = (string?)thumbnailItems[1]?["url"];

                if (thumbnailItems?.Count() >= 3)
                    thumbnails.HighResUrl = (string?)thumbnailItems[2]?["url"];

                if (thumbnailItems?.Count() >= 4)
                    thumbnails.StandardResUrl = (string?)thumbnailItems[3]?["url"];
                else if (thumbnails.MediumResUrl != null)
                    thumbnails.StandardResUrl = thumbnails.MediumResUrl;

                // Check if the text run element contains video count information
                if (textRun != null && Regex.IsMatch(textRun, @"^\d+ song[s]*"))
                {
                    videoCount = int.Parse(Regex.Replace(textRun, @"[^0-9]", ""));
                    author = artist;
                } // Text run element does not match and must be an album
                else
                {
                    // Set author value as artist and album
                    author = artist + " • " + textRun;
                    videoCount = null;
                }
            }
            catch
            {
                return null;
            }

            if (
                playlistId != null
                && title != null
                && author != null
                && thumbnails.StandardResUrl != null
            )
                return new Playlist(
                    playlistId,
                    title,
                    author,
                    description: null,
                    videoCount,
                    thumbnails
                );
            else
                return null;
        }

        /// <summary>
        /// Parses a duration string for a <see cref="Video"/>.
        /// </summary>
        /// <param name="durationString"></param>
        /// <returns>A <see cref="TimeSpan"/> representing the video duration.</returns>
        private static TimeSpan ParseDurationString(string? durationString) =>
            TimeSpan.Parse(
                durationString?.Count(x => x == ':') == 1
                    ? "00:" + durationString
                    : durationString ?? "0"
            );
    }
}
