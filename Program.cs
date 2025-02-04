class Program
{
    static async Task Main(string[] args)
    {
        const string DefaultRepo = "https://github.com/mohaimenhasan/flight-delay";

        var ghService = new GitHubService();
        Console.WriteLine($"Enter a public GitHub repository URL (e.g., {DefaultRepo}) - Press enter for default:");
        string? repoUrl = Console.ReadLine();

        if (string.IsNullOrEmpty(repoUrl))
        {
            repoUrl = DefaultRepo;
        }

        if (!Uri.TryCreate(repoUrl, UriKind.Absolute, out Uri? uri))
        {
            Console.WriteLine("Invalid URL.");
            return;
        }

        string[] urlParts = uri.AbsolutePath.Trim('/').Split('/');
        if (urlParts.Length < 2)
        {
            Console.WriteLine("Invalid GitHub repo format. Use: https://github.com/{owner}/{repo}");
            return;
        }

        string owner = urlParts[0];
        string repo = urlParts[1];

        string azureEndpoint = Environment.GetEnvironmentVariable("AZURE_ENDPOINT") 
            ?? throw new InvalidOperationException("Azure endpoint not configured."); string deploymentName = "gpt-4o";

        var openAIService = new AzureOpenAIService(azureEndpoint, deploymentName);

        Console.WriteLine($"Fetching files from {repo}...");
        var files = await ghService.GetRepoFilesAsync(owner, repo);
        Console.WriteLine($"Found {files.Count} files.");

        var fileContents = await ghService.GetRepoFileContentsAsync(owner, repo, files);
        Console.WriteLine("Retrieving relevant file contents...");

        Console.WriteLine("Analyzing repository with Azure OpenAI...");
        var yamlOutput = await openAIService.AnalyzeRepoAsync(files, fileContents);

        string yamlFilePath = "generated_setup.yaml";
        await File.WriteAllTextAsync(yamlFilePath, yamlOutput);

        Console.WriteLine($"\n=== Generated YAML ===\n{yamlOutput}");
        Console.WriteLine($"\nYAML file saved to {yamlFilePath}");
    }
}
