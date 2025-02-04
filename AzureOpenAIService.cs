﻿using System.Text;
using Azure.Core;
using Azure.Identity;
using Newtonsoft.Json;
using Azure.AI.OpenAI;

namespace RepoAnalyzer;

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
        string prompt = BuildPrompt(fileContents);
        HttpRequestMessage requestMessage = await GenerateRequestMessage(prompt);

        var response = await httpClient.SendAsync(requestMessage);
        var jsonResponse = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Azure OpenAI request failed: {response.StatusCode} - {response.ReasonPhrase}");
        }

        return HandleJsonResponseFromModel(jsonResponse);
    }

    private async Task<HttpRequestMessage> GenerateRequestMessage(string prompt)
    {
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

        var generatedBearerToken = await GetAccessTokenAsync();

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/openai/deployments/{deploymentName}/chat/completions?api-version=2024-02-15-preview")
        {
            Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
        };
        requestMessage.Headers.Add("Authorization", $"Bearer {generatedBearerToken}");
        return requestMessage;
    }

    private string BuildPrompt(Dictionary<string, string> fileContents, bool skipLargeFiles = false)
    {
        var prompt = "Analyze the following repository structure and generate a structured JSON output containing a YAML setup file that installs all dependencies in the repo:\n\n";

        int totalTokenCount = 0;
        const int tokenLimit = 2000;
        StringBuilder contentBuilder = new StringBuilder();

        foreach (var (file, content) in fileContents.OrderByDescending(f => f.Key)) // Prioritize key files
        {
            bool isImportant = IsImportantFile(file);
            int contentSize = content.Length;

            // If skipLargeFiles is enabled and file is non-important, skip large files
            if (skipLargeFiles && !isImportant && contentSize > 1000)
            {
                continue; // Skip this file entirely
            }

            // Approximate token count for the file content
            int estimatedTokens = contentSize / 4; // Roughly 1 token ≈ 4 characters

            // If adding this file exceeds the token limit, break
            if (totalTokenCount + estimatedTokens > tokenLimit)
            {
                break;
            }

            // Only clip content if it exceeds 500 characters (if needed)
            /*
            if (contentSize > 500)
            {
                contentSize = 500;
            }
            */

            contentBuilder.AppendLine($"## {file}\n{content.Substring(0, contentSize)}...");
            totalTokenCount += estimatedTokens;
        }

        prompt += contentBuilder.ToString();
        prompt += "\nBased on package and dependency files, infer required setup steps and return only a structured JSON output with a 'yaml' key containing the YAML string.\n";
        return prompt;
    }

    private static string HandleJsonResponseFromModel(string jsonResponse)
    {
        try
        {
            var responseObject = JsonConvert.DeserializeObject<dynamic>(jsonResponse);
            var messageContent = (responseObject?["choices"]?[0]?["message"]?["content"]?.ToString())
                ?? throw new Exception("Invalid response format: Missing 'content'.");

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

    // Determines if a file is important based on its extension
    private bool IsImportantFile(string file)
    {
        return file.EndsWith(".md")
            || file.EndsWith(".txt")
            || file.EndsWith(".json")
            || file.EndsWith(".yml")
            || file.EndsWith(".yaml")
            || file.EndsWith(".toml")
            || file.EndsWith(".xml")
            || file.EndsWith(".gradle");
    }

}
