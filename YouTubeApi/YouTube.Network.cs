using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json.Linq;

namespace YouTubeApi;

/// <summary>
/// Class to search for and retrieve playlist and video data from YouTube using the web client API.
/// </summary>
public static partial class YouTube
{
    public static class Network
    {
        private static readonly HttpClient HttpClient = new HttpClient(
            new HttpClientHandler
            {
                AutomaticDecompression =
                    System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            }
        );

        /// <summary>
        /// Sends an HTTP POST request containing data to the specified URI.
        /// </summary>
        /// <param name="uri">The URI to send a POST request to.</param>
        /// <param name="data">The <see cref="JObject"/> payload data to send.</param>
        /// <param name="visitorData">User identifier required by YouTube for some API requests.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to stop the operation.</param>
        /// <returns>The response from the POST request.</returns>
        public static async Task<string> SendPostRequest(
            string uri,
            JObject data,
            string? visitorData,
            CancellationToken? cancellationToken
        )
        {
            ConfigurePayloadContext(data, uri);

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(uri),
                Method = HttpMethod.Post,
                Content = new StringContent(data.ToString(), Encoding.UTF8, "application/json"),
            };

            ConfigureRequestHeaders(request, uri, visitorData);

            HttpResponseMessage? response;

            if (cancellationToken != null)
                response = await HttpClient.SendAsync(
                    request,
                    (CancellationToken)cancellationToken
                );
            else
                response = await HttpClient.SendAsync(request);

            var content = await response.Content.ReadAsStringAsync();

            return content;
        }

        /// <summary>
        /// Adds the relevant context data based on the request URI.
        /// </summary>
        /// <param name="data">The <see cref="JObject"/> payload data to send.</param>
        /// <param name="uri">The URI for the request.</param>
        private static void ConfigurePayloadContext(JObject data, string uri)
        {
            if (uri.Contains(Constants.PlayerEndpoint))
                data.Merge(GetYouTubeStreamInfoContextJson());
            else if (uri.Contains(Constants.YouTubeDomain))
                data.Merge(GetYouTubeContextJson());
            else
                data.Merge(GetYouTubeMusicContextJson());
        }

        /// <summary>
        /// Configures default and custom headers for requests to the YouTube API.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to configure the headers for.</param>
        /// <param name="uri">The URI for the request.</param>
        /// <param name="visitorData">User identifier required by YouTube for some API requests.</param>
        private static void ConfigureRequestHeaders(
            HttpRequestMessage request,
            string uri,
            string? visitorData
        )
        {
            string origin;

            if (uri.Contains(Constants.YouTubeDomain))
                origin = Constants.YouTubeUrl;
            else
                origin = Constants.YouTubeMusicUrl;

            if (request.Content != null)
            {
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Content.Headers.ContentEncoding.Add("gzip");
            }

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));

            request.Headers.Add("origin", origin);
            request.Headers.Add("x-goog-authuser", "0");
            request.Headers.Add("user-agent", Constants.UserAgent);

            if (visitorData != null)
                request.Headers.Add("x-goog-visitor-id", visitorData);
        }

        // Client configurations at https://github.com/yt-dlp/yt-dlp/blob/master/yt_dlp/extractor/youtube.py

        /// <summary>
        /// Returns the context JSON required by YouTube API requests.
        /// </summary>
        /// <returns>A <see cref="JObject"/> containing required context data.</returns>
        private static JObject GetYouTubeContextJson()
        {
            return JObject.Parse(
                @"{'context':{'client':{'clientName':'WEB','clientVersion':'2.20230928.04.00'}}}"
            );
        }

        /// <summary>
        /// Returns the context JSON required by YouTube Music API requests.
        /// </summary>
        /// <returns>A <see cref="JObject"/> containing required context data.</returns>
        private static JObject GetYouTubeMusicContextJson()
        {
            return JObject.Parse(
                @"{'context':{'client':{'clientName':'WEB_REMIX','clientVersion':'1.20220815.01.00'}}}"
            );
        }

        /// <summary>
        /// Returns the context JSON required for requesting stream info from YouTube.
        /// </summary>
        /// <returns>A <see cref="JObject"/> containing required context data.</returns>
        private static JObject GetYouTubeStreamInfoContextJson()
        {
            return JObject.Parse(
                @"{'context':{'client':{'clientName':'ANDROID','clientVersion':'18.11.34', 'androidSdkVersion': 30}}}"
            );
        }
    }
}
