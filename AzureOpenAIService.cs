using System.Text;
using Azure.Core;
using Azure.Identity;
using Newtonsoft.Json;

public class AzureOpenAIService
{
    private readonly string endpoint;
    private readonly string deploymentName;
    private readonly HttpClient httpClient;
    private readonly TokenCredential credential;

    public AzureOpenAIService(string endpoint, string deploymentName)
    {
        this.endpoint = endpoint;
        this.deploymentName = deploymentName;
        this.httpClient = new HttpClient();

        // Use DefaultAzureCredential for authentication
        this.credential = new DefaultAzureCredential();
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var tokenRequestContext = new TokenRequestContext(new[] { "https://cognitiveservices.azure.com/.default" });
        var accessToken = await credential.GetTokenAsync(tokenRequestContext, default);
        return accessToken.Token;
    }

    public async Task<string> AnalyzeRepoAsync(List<string> files, Dictionary<string, string> fileContents)
    {
        var prompt = "Analyze the following repository structure and generate a structured JSON output containing a YAML setup file that installs all dependencies in the repo:\n\n";

        foreach (var file in files)
        {
            prompt += $"- {file}\n";
        }

        prompt += "\nBased on package and dependency files, infer required setup steps and return only a structured JSON output with a 'yaml' key containing the YAML string.\n";

        // Append file contents (limit to avoid too many tokens)
        int count = 0;
        foreach (var (file, content) in fileContents)
        {
            if (++count > 3) break;
            prompt += $"\n## {file}\n{content.Substring(0, Math.Min(500, content.Length))}...\n";
        }

        var payload = new
        {
            model = deploymentName,
            messages = new object[]
            {
                new { role = "system", content = "You are an AI that generates YAML setup files for repositories." },
                new { role = "user", content = prompt }
            },
            temperature = 0.7,
            top_p = 0.95,
            max_tokens = 800,
            response_format = "json"
        };

        var token = await GetAccessTokenAsync();

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/openai/deployments/{deploymentName}/chat/completions?api-version=2024-02-15-preview")
        {
            Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
        };
        requestMessage.Headers.Add("Authorization", $"Bearer {token}");

        var response = await httpClient.SendAsync(requestMessage);
        var jsonResponse = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Azure OpenAI request failed: {response.StatusCode} - {response.ReasonPhrase}");
        }

        var responseObject = JsonConvert.DeserializeObject<dynamic>(jsonResponse);
        string yamlOutput = responseObject?.choices?[0]?.message?.content?.yaml?.ToString() ?? throw new Exception("Invalid response format: 'yaml' key not found.");

        return yamlOutput;
    }
}