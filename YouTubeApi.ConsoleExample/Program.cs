using YouTubeAPi;

Console.WriteLine("Enter a search query:");
var query = Console.ReadLine();

if (query == null)
{
    Console.WriteLine("Invalid search query.");
    return;
}

var results = await YouTube.SearchYouTube(query);

do
{
    Console.WriteLine($"PAGE {results.CurrentPage.PageNumber}");
    Console.WriteLine("----------------------------");

    foreach (var contentItem in results.CurrentPage.ContentItems)
    {
        if (contentItem.Content is YouTube.Video video)
            Console.WriteLine($"Video: {video.Title} - {video.Duration}");
        else if (contentItem.Content is YouTube.Playlist playlist)
            Console.WriteLine($"Playlist: {playlist.Title} - {playlist.VideoCount} videos");
    }

    if (!results.AllPagesFetched)
    {
        Console.WriteLine("Get another page of search results? (y/n)");
        var input = Console.ReadKey();

        if (input.Key != ConsoleKey.Y)
            break;

        Console.WriteLine();
        await results.GetNextPage();
    }
} while (!results.AllPagesFetched);
