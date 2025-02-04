using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class GitHubService
{
    private readonly HttpClient httpClient;

    private static readonly HashSet<string> ImportantFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        // Documentation & Licensing
        "README.md", "LICENSE", "CONTRIBUTING.md", "CODE_OF_CONDUCT.md", "SECURITY.md", "CHANGELOG.md", 
        
        // Python
        "requirements.txt", "Pipfile", "setup.py", "setup.cfg", "pyproject.toml", 
        
        // JavaScript / TypeScript
        "package.json", "yarn.lock", "tsconfig.json", "webpack.config.js", "rollup.config.js", 
        
        // Java / Kotlin
        "build.gradle", "build.gradle.kts", "pom.xml", "settings.gradle", "settings.gradle.kts", 
        
        // Ruby
        "Gemfile", "Gemfile.lock", "Rakefile", "config.ru", 
        
        // PHP
        "composer.json", "composer.lock", 
        
        // Rust
        "Cargo.toml", "Cargo.lock", 
        
        // Go
        "go.mod", "go.sum", "Gopkg.toml", "Gopkg.lock", 
        
        // C / C++ / CMake
        "CMakeLists.txt", "Makefile", "Makefile.am", "Makefile.in", 
        
        // Swift / iOS
        "Package.swift", "Cartfile", "Cartfile.resolved", 
        
        // Dart / Flutter
        "pubspec.yaml", "pubspec.lock", 
        
        // Configuration & CI/CD
        ".travis.yml", ".gitlab-ci.yml", ".circleci/config.yml", ".github/workflows/*.yml", 
        
        // Docker & Containers
        "Dockerfile", "docker-compose.yml", 
        
        // Infrastructure as Code
        "terraform.tf", "terraform.tfvars", "terraform.lock.hcl", "serverless.yml", 
        
        // Cloud Deployment
        "app.yaml", "firebase.json", "now.json", "vercel.json"
    };

    public GitHubService()
    {
        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "RepoAnalyzer");
    }

    public async Task<List<string>> GetRepoFilesAsync(string owner, string repo, string branch = "main")
    {
        var fileUrls = new List<string>();
        await GetRepoFilesRecursiveAsync(owner, repo, branch, "", fileUrls);
        return fileUrls;
    }

    private async Task GetRepoFilesRecursiveAsync(string owner, string repo, string branch, string path, List<string> fileUrls)
    {
        // Fetch all files in a single call using the Git Tree API
        var url = $"https://api.github.com/repos/{owner}/{repo}/git/trees/{branch}?recursive=1";

        var response = await httpClient.GetStringAsync(url);
        using var doc = JsonDocument.Parse(response);

        if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("tree", out JsonElement tree))
        {
            foreach (var file in tree.EnumerateArray())
            {
                string filePath = file.GetProperty("path").GetString();
                string type = file.GetProperty("type").GetString();

                if (type == "blob") // Ensure only files are added
                {
                    fileUrls.Add(filePath);
                }
            }
        }
    }

    public async Task<Dictionary<string, string>> GetRepoFileContentsNotHardCodedAsync(string owner, string repo, List<string> files, string branch = "main")
    {
        var fileContents = new Dictionary<string, string>();

        foreach (var file in files)
        {
            if (file.EndsWith(".md") || file.EndsWith(".txt") || file.EndsWith(".cs") || file.EndsWith(".js") || file.EndsWith(".json") || file.EndsWith(".yml") || file.EndsWith(".yaml") || file.EndsWith(".toml") || file.EndsWith(".lock") || file.EndsWith(".xml") || file.EndsWith(".gradle"))
            {
                try
                {
                    var url = $"https://raw.githubusercontent.com/{owner}/{repo}/{branch}/{file}";
                    var content = await httpClient.GetStringAsync(url);
                    fileContents[file] = content;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to fetch {file}: {ex.Message}");
                }
            }
        }
        return fileContents;
    }

    public async Task<Dictionary<string, string>> GetRepoFileContentsAsync(string owner, string repo, List<string> files, string branch = "main")
    {
        var fileContents = new Dictionary<string, string>();

        foreach (var file in files)
        {
            if (IsImportantFile(file))
            {
                try
                {
                    var url = $"https://raw.githubusercontent.com/{owner}/{repo}/{branch}/{file}";
                    var content = await httpClient.GetStringAsync(url);
                    fileContents[file] = content;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to fetch {file}: {ex.Message}");
                }
            }
        }
        return fileContents;
    }

    private bool IsImportantFile(string file)
    {
        if (ImportantFiles.Contains(file))
            return true;

        // Handle patterns like ".github/workflows/*.yml"
        if (file.StartsWith(".github/workflows/") && file.EndsWith(".yml"))
            return true;

        return false;
    }
}
