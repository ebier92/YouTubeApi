namespace YouTubeAPi;

/// <summary>
/// Class to search for and retrieve playlist and video data from YouTube using the web client API.
/// </summary>
public static partial class YouTube
{
    /// <summary>
    /// Class to store constant values.
    /// </summary>
    public static class Constants
    {
        // Domains and URLs
        public const string YouTubeDomain = "www.youtube.com";
        public const string YouTubeMusicDomain = "music.youtube.com";
        public const string YouTubeUrl = "https://" + YouTubeDomain;
        public const string YouTubeMusicUrl = "https://" + YouTubeMusicDomain;

        // API
        public const string BaseApi = "/youtubei/v1/";
        public const string SearchEndpoint = "search";
        public const string NextEndpoint = "next";
        public const string BrowseEndpoint = "browse";
        public const string PlayerEndpoint = "player";
        public const string ApiKey = "AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8";
        public const string ApiParameters = "?alt=json&prettyPrint=false&key=" + ApiKey;
        public const string YouTubeMusicChannelBrowseId = "UC-9-kyTW8ZkZNDHQJ6FgpwQ";

        // Header Data
        public const string UserAgent =
            "com.google.android.youtube/17.31.35 (Linux; U; Android 11) gzip";
    }
}
