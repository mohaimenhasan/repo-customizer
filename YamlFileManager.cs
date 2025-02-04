namespace RepoAnalyzer;

public static class YamlFileManager
{
    private static readonly string yamlDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "generated_yaml");

    public static async Task GenerateYamlFile(string repoName, string yamlOutput)
    {
        Directory.CreateDirectory(yamlDir);

        string indexFilePath = Path.Combine(yamlDir, $"index_{repoName}.txt");
        int fileIndex = GetNextFileIndex(indexFilePath);
        string yamlFilePath = Path.Combine(yamlDir, $"generated_setup_{repoName}_{fileIndex}.yaml");

        await File.WriteAllTextAsync(yamlFilePath, yamlOutput);
        File.WriteAllText(indexFilePath, (fileIndex + 1).ToString());

        Console.WriteLine($"\n=== Generated YAML ===\n{yamlOutput}");
        Console.WriteLine($"YAML file saved to {yamlFilePath}");
    }

    private static int GetNextFileIndex(string indexFilePath)
    {
        if (File.Exists(indexFilePath))
        {
            string content = File.ReadAllText(indexFilePath).Trim();
            if (int.TryParse(content, out int index))
            {
                return index;
            }
        }

        File.WriteAllText(indexFilePath, "1");
        return 1;
    }
}
