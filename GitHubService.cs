using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class GitHubService
{
    private readonly HttpClient httpClient;

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
        var url = string.IsNullOrEmpty(path) ?
            $"https://api.github.com/repos/{owner}/{repo}/git/trees/{branch}?recursive=1" :
            $"https://api.github.com/repos/{owner}/{repo}/contents/{path}?ref={branch}";

        var response = await httpClient.GetStringAsync(url);
        using var doc = JsonDocument.Parse(response);

        if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("tree", out JsonElement tree))
        {
            foreach (var file in tree.EnumerateArray())
            {
                string filePath = file.GetProperty("path").GetString();
                string type = file.GetProperty("type").GetString();

                if (type == "blob")
                {
                    fileUrls.Add(filePath);
                }
                else if (type == "tree")
                {
                    await GetRepoFilesRecursiveAsync(owner, repo, branch, filePath, fileUrls);
                }
            }
        }
        else if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var file in doc.RootElement.EnumerateArray())
            {
                string filePath = file.GetProperty("path").GetString();
                string type = file.GetProperty("type").GetString();

                if (type == "file")
                {
                    fileUrls.Add(filePath);
                }
                else if (type == "dir")
                {
                    await GetRepoFilesRecursiveAsync(owner, repo, branch, filePath, fileUrls);
                }
            }
        }
    }

    public async Task<Dictionary<string, string>> GetRepoFileContentsAsync(string owner, string repo, List<string> files, string branch = "main")
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
}
