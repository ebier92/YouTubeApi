namespace YouTubeAPi;

/// <summary>
/// Class to search for and retrieve playlist and video data from YouTube using the web client API.
/// </summary>
public static partial class YouTube
{
    /// <summary>
    /// Class representing a set of thumbnails for a YouTube video.
    /// </summary>
    public class Thumbnails
    {
        private string? customLowResUrl;
        private string? customMediumResUrl;
        private string? customHighResUrl;
        private string? customStandardResUrl;

        /// <summary>
        /// The video ID for the thumbmails.
        /// </summary>
        public string? VideoId { get; set; }

        /// <summary>
        /// The low resolution thumbnail option.
        /// </summary>
        public string? LowResUrl
        {
            get
            {
                if (VideoId != null)
                    return $"https://img.youtube.com/vi/{VideoId}/default.jpg";
                else if (customLowResUrl != null)
                    return customLowResUrl;
                else
                    return null;
            }
            set { customLowResUrl = value; }
        }

        /// <summary>
        /// The medium resolution thumbnail option.
        /// </summary>
        public string? MediumResUrl
        {
            get
            {
                if (VideoId != null)
                    return $"https://img.youtube.com/vi/{VideoId}/mqdefault.jpg";
                else if (customMediumResUrl != null)
                    return customMediumResUrl;
                else
                    return null;
            }
            set { customMediumResUrl = value; }
        }

        /// <summary>
        /// The high resolution thumbnail option.
        /// </summary>
        public string? HighResUrl
        {
            get
            {
                if (VideoId != null)
                    return $"https://img.youtube.com/vi/{VideoId}/hqdefault.jpg";
                else if (customHighResUrl != null)
                    return customHighResUrl;
                else
                    return null;
            }
            set { customHighResUrl = value; }
        }

        /// <summary>
        /// The standard resolution thumbnail option.
        /// </summary>
        public string? StandardResUrl
        {
            get
            {
                if (VideoId != null)
                    return $"https://img.youtube.com/vi/{VideoId}/sddefault.jpg";
                else if (customStandardResUrl != null)
                    return customStandardResUrl;
                else
                    return null;
            }
            set { customStandardResUrl = value; }
        }
    }
}
