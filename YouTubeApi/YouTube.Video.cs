namespace YouTubeApi;

/// <summary>
/// Class to search for and retrieve playlist and video data from YouTube using the web client API.
/// </summary>
public static partial class YouTube
{
    /// <summary>
    /// Class representing a video from YouTube.
    /// </summary>
    public class Video
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Video"/> class.
        /// </summary>
        /// <param name="videoId">The video ID of the video.</param>
        /// <param name="title">The title of the video.</param>
        /// <param name="author">The author (channel name) of the video.</param>
        /// <param name="duration">The duration of the video.</param>
        public Video(string videoId, string title, string author, TimeSpan duration)
        {
            VideoId = videoId;
            Title = title;
            Author = author;
            Duration = duration;
            Thumbnails = new Thumbnails { VideoId = videoId, };
        }

        /// <summary>
        /// The video ID of the video.
        /// </summary>
        public string VideoId { get; }

        /// <summary>
        /// The video's watch endpoint URL.
        /// </summary>
        public string Url
        {
            get { return $"https://www.youtube.com/watch?v={VideoId}"; }
        }

        /// <summary>
        /// The video title.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// The video "author" (originating YouTube channel name).
        /// This usually matches the artist's name for videos on YouTube Music.
        /// </summary>
        public string Author { get; }

        /// <summary>
        /// The video's duration.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// The thumbnail set for the video.
        /// </summary>
        public Thumbnails Thumbnails { get; }
    }
}
