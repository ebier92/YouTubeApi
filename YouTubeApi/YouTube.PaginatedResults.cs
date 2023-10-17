using Newtonsoft.Json.Linq;

namespace YouTubeApi;

/// <summary>
/// Class to search for and retrieve playlist and video data from YouTube using the web client API.
/// </summary>
public static partial class YouTube
{
    /// <summary>
    /// Class to handle pagination of YouTube API results.
    /// </summary>
    public class PaginatedResults
    {
        public delegate Task<JToken?> SendPageRequest(
            string? continuationToken,
            string? visitorData,
            CancellationToken? cancellationToken
        );
        public delegate Page ParsePageContentItems(JToken? jsonResponse, int pageNumber);
        private int currentPageNumber = 0;
        private bool allPagesFetched = false;
        private string? continuationToken;
        private string? visitorData;
        private Dictionary<int, Page> pages = new Dictionary<int, Page>();
        private SendPageRequest sendPageRequest;
        private ParsePageContentItems parsePageContentItems;

        /// <summary>
        /// Initializes an instance of the <see cref="PaginatedResults"/> class.
        /// </summary>
        /// <param name="sendPageRequest">A delegate function to handle fetching of page results.</param>
        /// <param name="parsePageContentItems">A delegate function to handle extraction of content from the response.</param>
        public PaginatedResults(
            SendPageRequest sendPageRequest,
            ParsePageContentItems parsePageContentItems
        )
        {
            this.sendPageRequest = sendPageRequest;
            this.parsePageContentItems = parsePageContentItems;
        }

        /// <summary>
        /// Gets the most recently fetched <see cref="Page"/>.
        /// </summary>
        public Page CurrentPage
        {
            get { return pages[currentPageNumber]; }
        }

        /// <summary>
        /// Flag to indicate when all pages of content have been fetched.
        /// </summary>
        public bool AllPagesFetched
        {
            get { return allPagesFetched; }
        }

        /// <summary>
        /// Retrieves the next <see cref="Page"/> of content after the current page.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel loading of the next page.</param>
        /// <returns>The next <see cref="Page"/>.</returns>
        public async Task<Page> GetNextPage(CancellationToken? cancellationToken = null)
        {
            if (allPagesFetched)
                return pages[currentPageNumber];

            var response = await sendPageRequest(continuationToken, visitorData, cancellationToken);

            if (response == null)
                return new Page(currentPageNumber);

            var newContinuationToken = JsonParsers.ExtractContinuationToken(response);
            var newVisitorData = JsonParsers.ExtractVisitorData(response);

            if (newContinuationToken != null)
                continuationToken = newContinuationToken;
            else
                allPagesFetched = true;

            if (newVisitorData != null)
                visitorData = newVisitorData;

            currentPageNumber++;
            var page = parsePageContentItems(response, currentPageNumber);
            pages[currentPageNumber] = page;

            return page;
        }

        /// <summary>
        /// Retrieves a specific page number either from cached pages, or by making sucessive requests.
        /// </summary>
        /// <param name="pageNumber">The page number.</param>
        /// <param name="cancellationToken">A cancellation token to cancel loading of pages.</param>
        /// <returns>The specified <see cref="Page"/>.</returns>
        public async Task<Page> GetPage(int pageNumber, CancellationToken? cancellationToken = null)
        {
            if (pageNumber < 1)
                throw new ArgumentOutOfRangeException(
                    "The `pageNumber` argument must be a value of 1 or greater."
                );

            if (pageNumber <= currentPageNumber)
                return pages[pageNumber];

            if (allPagesFetched && pageNumber > currentPageNumber)
                return pages[currentPageNumber];

            Page page = await GetNextPage(cancellationToken);

            while (
                currentPageNumber < pageNumber
                && (
                    cancellationToken == null
                    || !((CancellationToken)cancellationToken).IsCancellationRequested
                )
            )
            {
                var initialCurrentPage = currentPageNumber;
                page = await GetNextPage(cancellationToken);

                if (currentPageNumber == initialCurrentPage)
                    return page;
            }

            return page;
        }
    }
}
