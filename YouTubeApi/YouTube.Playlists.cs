namespace YouTubeApi;

/// <summary>
/// Class to search for and retrieve playlist and video data from YouTube using the web client API.
/// </summary>
public static partial class YouTube
{
    /// <summary>
    /// Class representing a playlist from YouTube.
    /// </summary>
    public class Playlist
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Playlist"/> class.
        /// </summary>
        /// <param name="playlistId">The playlist ID of the playlist.</param>
        /// <param name="title">The title of the playlist.</param>
        /// <param name="author">The author (channel name) of the playlist.</param>
        /// <param name="description">A description of the playlist.</param>
        /// <param name="videoCount">The number of videos in the playlist.</param>
        /// <param name="thumbnails">A <see cref="Thumbnails"/> set to use for the playlist.</param>
        public Playlist(
            string playlistId,
            string title,
            string? author,
            string? description,
            int? videoCount,
            Thumbnails thumbnails
        )
        {
            PlaylistId = playlistId;
            Title = title;
            Author = author;
            Description = description;
            VideoCount = videoCount;
            Thumbnails = thumbnails;
        }

        /// <summary>
        /// The playlist ID of the playlist.
        /// </summary>
        public string PlaylistId { get; }

        /// <summary>
        /// The playlist title.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// The playlist "author" (orginating YouTube channel name).
        /// Usually matches the artist's name for album playlists on YouTube Music.
        /// </summary>
        public string? Author { get; }

        /// <summary>
        /// The playlist description.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// The number of videos in the playlist.
        /// </summary>
        public int? VideoCount { get; }

        /// <summary>
        /// The thumbnail set for the playlist.
        /// </summary>
        public Thumbnails Thumbnails { get; }
    }
}
