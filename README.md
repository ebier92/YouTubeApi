# Introduction

`YouTubeApi` is a music-focused client interface for the internal API's of YouTube and YouTube Music. Some of it's functionality includes:

- Performing searches
- Getting raw audio or video stream URL's
- Viewing playlists
- Getting related videos
- Getting infinite "watch" playlists for music or related video content

# Installation

```
dotnet add package YouTubeApi
```

# Usage

All package functionality is exposed through the static `YouTube` class.

```cs
using YouTubeApi;

var results = await YouTube.SearchYouTube(query);
```

## Working with YouTube Content

Content returned from YouTube or YouTube Music API calls will be provided as a `ContentItem`. A `ContentItem` can hold the more specific content types of `Video` or `Playlist` through it's `Content` property. Consumers of `ContentItems` will typically need to check for the type of `Content` before working with it.

```cs
if (contentItem.Content is YouTube.Video video)
{
    // Do something with a video.
}
else if (contentItem.Content is YouTube.Playlist playlist)
{
    // Do something with a playlist.
}
```

The `Video` class contains the following properties:
- `VideoId`: The unique YouTube ID for the video.
- `Url`: A full URL to link directly to the YouTube video.
- `Title`: The video title.
- `Author`: The video's originating YouTube channel name.
- `Duration`: The video's duration.
- `Thumbnails`: A data class to hold URL's to thumbnail images of varying image quality.

The `Playlist` class contains the following properties:
- `PlaylistId`: The unique YouTube ID for the playlist.
- `Title`: The playlist title.
- `Author`: The playlist's originating YouTube channel name.
- `Description`: The playlist description.
- `VideoCount`: The number of videos in the playlist.
- `Thumbnails`: A data class to hold URL's to thumbnail images of varying image quality.

**Note**: Depending on the method used to retrieve a `Video` or a `Playlist`, certain properties may default to `null`.

## Working with Paginated Results

Certain methods, such as `SearchYouTube`, `SearchYouTubeMusic`, `GetPlaylistVideos`, etc. need to handle paginated result content from YouTube. These methods return a `PaginatedResults` class to manage this.

```cs
var playlistUrl = "https://www.youtube.com/playlist?list=PL7P2TR060DnTGVmYkylwASt5RfS8nlvcu";
var playlistId = YouTube.GetPlaylistId(playlistUrl);

// Returns a PaginatedResults class with the first page of content pre-fetched
var pages = await YouTube.GetPlaylistVideos(playlistId);

// Returns the last fetched page
var firstPage = pages.CurrentPage;

// Fetch next page
var nextPage = await pages.GetNextPage();
// Fetch specific page by number, will return a cached page if the page number has already been fetched
var thirdPage = await pages.GetPage(3);


// Fetch pages until complete
while (!pages.AllPagesFetched)
{
    await pages.GetNextPage();

    foreach (var contentItem in pages.CurrentPage.ContentItems)
    {
        // Do something with page content
    }
}
```

## Getting Curated Music Content

YouTube lists curated playlists and song selections on their official music channel home page. The `GetHomePageContentSections` method returns a list of `HomePageSection`s which hold the section name text and the related content items.

```cs
var homePageSections = await YouTube.GetHomePageContentSections();

// Access section name
var sectionName = homePageSections[0].SectionName;
// Access section content items (can be videos, playlists, or both)
var sectionContent = homePageSections[0].ContentItems;
```

## Performing Searches

`YouTubeApi` supports raw text searches into YouTube:

```cs
var pages = await YouTube.SearchYouTube("search query");
```

It also supports searches into YouTube Music filtered by content type using `YouTube.MusicSearchFilter`. Available filters include `Songs`, `FeaturedPlaylists`, `CommunityPlaylists`, and `Albums`.

```cs
// Retrieves song content (as Videos) by Pink Floyd
var pages = await YouTube.SearchYouTubeMusic("pink floyd", YouTube.MusicSearchFilter.Songs);

// Retrieves album content (as Playlists) by the Beatles
var pages = await YouTube.SearchYouTubeMusic("the beatles", YouTube.MusicSearchFilter.Albums);
```

## Getting Videos in a Playlist

The `GetPlaylistVideos` retrieves pages of content for the specified playlist.

```cs
var pages = await YouTube.GetPlaylistVideos(playlistId);

// Get all pages of playlist content
while (!pages.AllPagesFetched)
{
    await pages.GetNextPage();
}
```

## Getting Videos from Generated Lists

YouTube and YouTube Music both have the concept of infinite scrolling lists of related content with the "related videos" and "watch playlist" features respectively. These can be accessed via the `GetRelatedVideos` and `GetWatchPlaylist` methods.

```cs
// Gets video focused content from YouTube
var pages = await YouTube.GetRelatedVideos("mQER0A0ej0M");

// Gets music focused content (audio only song videos) from YouTube Music
var pages = await YouTube.GetWatchPlaylist("mQER0A0ej0M");
```

## Getting Video Stream URLs

YouTube uses various URLs behind the scenes to serve raw video or raw audio content. These URL's can be accessed via `GetStreamInfo`.

```cs
var streams = await YouTube.GetStreamInfo("mQER0A0ej0M");

// Get only audio/mp4 streams
var audioMp4Streams = streams.Where(x => x.MimeType.Contains("audio/mp4"));
```