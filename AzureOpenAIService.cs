using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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
            messages = new object[]
            {
                new { role = "system", content = "You are an AI that generates YAML setup files for repositories." },
                new { role = "user", content = prompt }
            },
            temperature = 0.7,
            top_p = 0.95,
            max_tokens = 800,
            stream = false
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

        try
        {
            var responseObject = JsonConvert.DeserializeObject<dynamic>(jsonResponse);
            var messageContent = responseObject["choices"][0]["message"]["content"].ToString();

            // Extract JSON block from message content
            int jsonStart = messageContent.IndexOf("{"), jsonEnd = messageContent.LastIndexOf("}");
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                string extractedJson = messageContent.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var yamlObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(extractedJson);

                if (yamlObject != null && yamlObject.ContainsKey("yaml"))
                {
                    return yamlObject["yaml"];
                }
            }
            throw new Exception("Invalid response format: Missing 'yaml' key.");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse response: {ex.Message}");
        }
    }
}
