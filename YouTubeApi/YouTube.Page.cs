namespace YouTubeAPi;

/// <summary>
/// Class to search for and retrieve playlist and video data from YouTube using the web client API.
/// </summary>
public static partial class YouTube
{
    /// <summary>
    /// Class representing a page of results of <see cref="ContentItem"/>s along with associated metadata.
    /// </summary>
    public class Page
    {
        /// <summary>
        /// Initializes an instance of the <see cref="Page"/> class.
        /// </summary>
        /// <param name="pageNumber">The page number.</param>
        public Page(int pageNumber)
        {
            PageNumber = pageNumber;
            ContentItems = new List<ContentItem>();
        }

        /// <summary>
        /// The page number of the <see cref="Page"/>.
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// The <see cref="ContentItems"/> from the page.
        /// </summary>
        public List<ContentItem> ContentItems { get; }
    }
}
