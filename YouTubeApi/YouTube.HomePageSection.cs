namespace YouTubeApi;

/// <summary>
/// Class to search for and retrieve playlist and video data from YouTube using the web client API.
/// </summary>
public static partial class YouTube
{
    /// <summary>
    /// Class representing a section of content items from the YouTube main music home page.
    /// </summary>
    public class HomePageSection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HomePageSection"/> class.
        /// </summary>
        /// <param name="sectionName"></param>
        public HomePageSection(string sectionName)
        {
            SectionName = sectionName;
            ContentItems = new List<ContentItem>();
        }

        /// <summary>
        /// The name of the content section from the music channel home page.
        /// </summary>
        public string SectionName { get; }

        /// <summary>
        /// The list of <see cref="ContentItems"/> from the section.
        /// </summary>
        public List<ContentItem> ContentItems { get; }
    }
}
