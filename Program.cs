using RepoAnalyzer;

class Program
{
    private const string DefaultDeploymentName = "gpt-4o";

    static async Task Main(string[] args)
    {
        string repoUrl = GitHubRepositoryHelper.GetRepositoryUrl();
        if (!GitHubRepositoryHelper.TryParseRepository(repoUrl, out string owner, out string repo))
        {
            Console.WriteLine("Invalid GitHub repository URL.");
            return;
        }

        var ghService = new GitHubService();
        Console.WriteLine($"Fetching files from {repo}...");
        var files = await ghService.GetRepoFilesAsync(owner, repo);
        Console.WriteLine($"Found {files.Count} files.");

        var fileContents = await ghService.GetRepoFileContentsAsync(owner, repo, files);
        Console.WriteLine("Retrieving relevant file contents...");

        string azureEndpoint = Environment.GetEnvironmentVariable("AZURE_ENDPOINT")
            ?? throw new InvalidOperationException("❌ Azure endpoint not configured.");
        string deploymentName = Environment.GetEnvironmentVariable("AZURE_DEPLOYMENT_NAME") ?? DefaultDeploymentName;

        var openAIService = new AzureOpenAIService(azureEndpoint, deploymentName);
        Console.WriteLine("Analyzing repository with Azure OpenAI...");

        var yamlOutput = await openAIService.AnalyzeRepoAsync(files, fileContents);

        await YamlFileManager.GenerateYamlFile(repo, yamlOutput);
    }
}
