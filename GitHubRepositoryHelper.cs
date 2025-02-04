namespace RepoAnalyzer;

public static class GitHubRepositoryHelper
{
    private const string DefaultRepo = "https://github.com/mohaimenhasan/flight-delay";

    public static string GetRepositoryUrl()
    {
        Console.WriteLine($"Enter a public GitHub repository URL (e.g., {DefaultRepo}) - Press enter for default:");
        string? inputUrl = Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(inputUrl) ? DefaultRepo : inputUrl;
    }

    public static bool TryParseRepository(string repoUrl, out string owner, out string repo)
    {
        owner = repo = string.Empty;

        if (!Uri.TryCreate(repoUrl, UriKind.Absolute, out Uri? uri) || uri.Host != "github.com")
        {
            return false;
        }

        string[] urlParts = uri.AbsolutePath.Trim('/').Split('/');
        if (urlParts.Length < 2)
        {
            return false;
        }

        owner = urlParts[0];
        repo = urlParts[1];
        return true;
    }
}