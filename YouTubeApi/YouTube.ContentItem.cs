namespace YouTubeAPi;

/// <summary>
/// Class to search for and retrieve playlist and video data from YouTube using the web client API.
/// </summary>
public static partial class YouTube
{
    /// <summary>
    /// Class to represent an item returned from a mixed content collection.
    /// </summary>
    public class ContentItem
    {
        private readonly Video? video;
        private readonly Playlist? playlist;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentItem"/> class.
        /// </summary>
        /// <param name="video">A <see cref="Video"/> to store in the item.</param>
        public ContentItem(Video? video)
        {
            this.video = video;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentItem"/> class.
        /// </summary>
        /// <param name="playlist">A <see cref="Playlist"/> to store in the item.</param>
        public ContentItem(Playlist? playlist)
        {
            this.playlist = playlist;
        }

        /// <summary>
        /// Gets the <see cref="Video"/> or <see cref="Playlist"/> content depending on the type of data that was assigned.
        /// </summary>
        public object? Content
        {
            get
            {
                if (video != null)
                    return video;
                else if (playlist != null)
                    return playlist;
                else
                    return null;
            }
        }
    }
}
