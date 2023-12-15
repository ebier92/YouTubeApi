namespace YouTubeApi.Tests;

using YouTubeApi;

[TestClass]
public class YouTubeApiSpecs
{
    private void IsValidThumbnails(YouTube.Thumbnails thumbnails)
    {
        Assert.IsTrue(
            thumbnails.LowResUrl?.Length > 0
                || thumbnails.StandardResUrl?.Length > 0
                || thumbnails.MediumResUrl?.Length > 0
                || thumbnails.HighResUrl?.Length > 0
        );
    }

    private void IsValidVideo(YouTube.Video video)
    {
        Assert.IsTrue(video.VideoId.Length > 0);
        Assert.IsTrue(video.Url.Length > 0);
        Assert.IsTrue(video.Title.Length > 0);
        Assert.IsTrue(video.Author.Length > 0);
        Assert.IsTrue(video.Duration.TotalMilliseconds > 0);
        IsValidThumbnails(video.Thumbnails);
    }

    private void IsValidPlaylist(YouTube.Playlist playlist)
    {
        Assert.IsTrue(playlist.PlaylistId.Length > 0);
        Assert.IsTrue(playlist.Title.Length > 0);
        Assert.IsTrue(playlist.Author == null || playlist.Author.Length > 0);
        Assert.IsTrue(playlist.Description == null || playlist.Description.Length > 0);
        Assert.IsTrue(playlist.VideoCount == null || playlist.VideoCount > 0);
        IsValidThumbnails(playlist.Thumbnails);
    }

    private void IsValidContent(YouTube.ContentItem contentItem)
    {
        if (contentItem.Content is YouTube.Video video)
            IsValidVideo(video);
        else if (contentItem.Content is YouTube.Playlist playlist)
            IsValidPlaylist(playlist);
        else
            throw new Exception("Unknown content item type.");
    }

    [TestMethod]
    public async Task GetHomePageContentItems_LoadsHomePage_ExtractsHomePageSections()
    {
        var homepageSections = await YouTube.GetHomePageContentSections();

        Assert.IsTrue(homepageSections.Count > 0);

        foreach (var homepageSection in homepageSections)
        {
            Assert.IsTrue(homepageSection.SectionName.Length > 0);
            Assert.IsTrue(homepageSection.ContentItems.Count > 0);

            homepageSection.ContentItems.ForEach(IsValidContent);
        }
    }

    [DataTestMethod]
    [DataRow("https://www.youtube.com/watch?v=mQER0A0ej0M")]
    [DataRow("https://www.youtube.com/watch?v=q-JJf7S0osI&pp=ygUKcm9jayBtdXNpYw%3D%3D")]
    [DataRow("https://www.youtube.com/watch?v=S-OgkNgxm3k&pp=ygUKcm9jayBtdXNpYw%3D%3D")]
    public async Task GetStreamInfo_VideoUrlsInput_RetrievesStreamInfo(string url)
    {
        var streamInfos = await YouTube.GetStreamInfo(YouTube.GetVideoId(url));

        Assert.IsTrue(streamInfos.Count() > 0);

        foreach (var streamInfo in streamInfos)
        {
            Assert.IsTrue(streamInfo.Url.Length > 0);
            Assert.IsTrue(streamInfo.MimeType.Length > 0);
            Assert.IsTrue(streamInfo.BitRate > 0);
        }
    }

    [TestMethod]
    public async Task SearchYouTube_SearchesForQuery_RetrievesMultiplePagesOfValidContent()
    {
        var pages = await YouTube.SearchYouTube("the beatles");

        for (int i = 1; i <= 3; i++)
        {
            var page = await pages.GetPage(i);

            Assert.AreEqual(pages.CurrentPage.PageNumber, i);
            Assert.AreEqual(page.PageNumber, i);
            Assert.IsTrue(page.ContentItems.Count > 0);

            foreach (var contentItem in page.ContentItems)
            {
                IsValidContent(contentItem);
            }
        }
    }

    [DataTestMethod]
    [DataRow(YouTube.MusicSearchFilter.Songs)]
    [DataRow(YouTube.MusicSearchFilter.Albums)]
    [DataRow(YouTube.MusicSearchFilter.FeaturedPlaylists)]
    [DataRow(YouTube.MusicSearchFilter.CommunityPlaylists)]
    public async Task SearchYouTubeMusic_SearchesForQueryWithFilter_RetrievesMultiplePagesOfValidExpectedContent(
        YouTube.MusicSearchFilter filter
    )
    {
        var pages = await YouTube.SearchYouTubeMusic("the beatles", filter);

        for (int i = 1; i <= 3; i++)
        {
            if (pages.AllPagesFetched)
                break;

            var page = await pages.GetPage(i);

            Assert.AreEqual(pages.CurrentPage.PageNumber, i);
            Assert.AreEqual(page.PageNumber, i);
            Assert.IsTrue(page.ContentItems.Count > 0);

            foreach (var contentItem in page.ContentItems)
            {
                if (
                    filter == YouTube.MusicSearchFilter.Songs
                    && contentItem.Content is YouTube.Video video
                )
                    IsValidVideo(video);
                else if (
                    filter != YouTube.MusicSearchFilter.Songs
                    && contentItem.Content is YouTube.Playlist playlist
                )
                    IsValidPlaylist(playlist);
                else
                    throw new Exception("Invalid content type for input filter");
            }
        }
    }

    [DataTestMethod]
    [DataRow("https://www.youtube.com/watch?v=zOILAZHf2pE")]
    [DataRow("https://www.youtube.com/watch?v=YChioDnHxm4")]
    [DataRow("https://www.youtube.com/watch?v=QdBZY2fkU-0")]
    public async Task GetRelatedVideos_VideoUrlsInput_Retrieves_MultiplePagesOfRelatedVideos(
        string url
    )
    {
        var pages = await YouTube.GetRelatedVideos(YouTube.GetVideoId(url));

        for (int i = 1; i <= 3; i++)
        {
            var page = await pages.GetPage(i);

            Assert.AreEqual(pages.CurrentPage.PageNumber, i);
            Assert.AreEqual(page.PageNumber, i);
            Assert.IsTrue(page.ContentItems.Count > 0);

            foreach (var contentItem in page.ContentItems)
            {
                if (contentItem.Content is YouTube.Video video)
                    IsValidVideo(video);
                else
                    throw new Exception("Invalid content type.");
            }
        }
    }

    [DataTestMethod]
    [DataRow("https://www.youtube.com/playlist?list=PL7P2TR060DnTGVmYkylwASt5RfS8nlvcu")]
    [DataRow(
        "https://www.youtube.com/watch?v=BMMGwtklEeE&list=RDCLAK5uy_l3PeyHeqJh1dR78WjfsMJwRHJx9ofMvvc&start_radio=1"
    )]
    [DataRow(
        "https://www.youtube.com/watch?v=nN120kCiVyQ&list=RDCLAK5uy_nZiG9ehz_MQoWQxY5yElsLHCcG0tv9PRg"
    )]
    public async Task GetPlaylistVideos_PlaylistUrlsAsInput_RetrievesAllPagesOfPlaylistVideos(
        string url
    )
    {
        var pages = await YouTube.GetPlaylistVideos(YouTube.GetPlaylistId(url));

        while (!pages.AllPagesFetched)
        {
            var page = await pages.GetNextPage();

            Assert.IsTrue(page.ContentItems.Count > 0);

            foreach (var contentItem in page.ContentItems)
            {
                if (contentItem.Content is YouTube.Video video)
                    IsValidVideo(video);
                else
                    throw new Exception("Invalid content type.");
            }
        }
    }

    [DataTestMethod]
    [DataRow("https://www.youtube.com/watch?v=mQER0A0ej0M")]
    [DataRow("https://www.youtube.com/watch?v=q-JJf7S0osI&pp=ygUKcm9jayBtdXNpYw%3D%3D")]
    [DataRow("https://www.youtube.com/watch?v=S-OgkNgxm3k&pp=ygUKcm9jayBtdXNpYw%3D%3D")]
    public async Task GetWatchPlaylistVideos_VideoUrlsAsInput_RetrievesMultiplePagesOfVideos(
        string url
    )
    {
        var pages = await YouTube.GetWatchPlaylistVideos(YouTube.GetVideoId(url));

        for (int i = 1; i <= 3; i++)
        {
            var page = await pages.GetPage(i);

            Assert.AreEqual(pages.CurrentPage.PageNumber, i);
            Assert.AreEqual(page.PageNumber, i);
            Assert.IsTrue(page.ContentItems.Count > 0);

            foreach (var contentItem in page.ContentItems)
            {
                if (contentItem.Content is YouTube.Video video)
                    IsValidVideo(video);
                else
                    throw new Exception("Invalid content type.");
            }
        }
    }

    [DataTestMethod]
    [DataRow("https://www.youtube.com/watch?v=mQER0A0ej0M", "mQER0A0ej0M")]
    [DataRow(
        "https://www.youtube.com/watch?v=q-JJf7S0osI&pp=ygUKcm9jayBtdXNpYw%3D%3D",
        "q-JJf7S0osI"
    )]
    [DataRow(
        "https://www.youtube.com/watch?v=S-OgkNgxm3k&pp=ygUKcm9jayBtdXNpYw%3D%3D",
        "S-OgkNgxm3k"
    )]
    public void GetVideoId_VideoUrlsInput_ExtractsVideoId(string url, string videoId)
    {
        Assert.AreEqual(YouTube.GetVideoId(url), videoId);
    }

    [DataTestMethod]
    [DataRow(
        "https://www.youtube.com/playlist?list=PL7P2TR060DnTGVmYkylwASt5RfS8nlvcu",
        "PL7P2TR060DnTGVmYkylwASt5RfS8nlvcu"
    )]
    [DataRow(
        "https://www.youtube.com/watch?v=BMMGwtklEeE&list=RDCLAK5uy_l3PeyHeqJh1dR78WjfsMJwRHJx9ofMvvc&start_radio=1",
        "RDCLAK5uy_l3PeyHeqJh1dR78WjfsMJwRHJx9ofMvvc"
    )]
    [DataRow(
        "https://www.youtube.com/watch?v=CqnU_sJ8V-E&list=PLw0ht2-AVRxXrr5SaVjkMZf99NmyIL5HE",
        "PLw0ht2-AVRxXrr5SaVjkMZf99NmyIL5HE"
    )]
    public void GetPlaylistId_PlaylistUrlsInput_ExtractsPlaylistId(string url, string playlistId)
    {
        Assert.AreEqual(YouTube.GetPlaylistId(url), playlistId);
    }
}
