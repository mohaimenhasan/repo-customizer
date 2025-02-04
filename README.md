# RepoAnalyzer

RepoAnalyzer is a .NET console application that analyzes a public GitHub repository by retrieving all files and leveraging **Azure OpenAI** to generate a YAML file that installs all dependencies.

## Features
- Fetches all files in a given public GitHub repository.
- Extracts relevant content from key files (`README.md`, `CONTRIBUTOR.md`).
- Detects all file extensions to infer programming languages.
- Uses **Azure OpenAI (GPT-4 Turbo)** to generate a **YAML setup file** for dependency installation.

## Getting Started

### Prerequisites
- **.NET 9 or later** installed ([Download](https://dotnet.microsoft.com/en-us/download))
- **GitHub API access** (public repos only)
- **Azure OpenAI API Key and Endpoint**

### Installation
1. Clone the repository:
   ```sh
   git clone https://github.com/mohaimenhasan/repo-customizer.git
   cd repo-customizer
   ```

2. Install dependencies:
   ```sh
   dotnet restore
   ```

### Configuration
1. **Set Up Azure OpenAI**:
   - Go to [Azure OpenAI Studio](https://oai.azure.com/).
   - Deploy **GPT-4 Turbo** or another model.
   - Retrieve your **API key**, **endpoint**, and **deployment name**.

2. **Set environment variables** (Recommended):
   ```sh
   export AZURE_OPENAI_ENDPOINT="https://your-azure-openai-resource.openai.azure.com"
   export AZURE_OPENAI_DEPLOYMENT="gpt-4-turbo"
   ```

### Usage
Run the application and enter a GitHub repository URL:
```sh
   dotnet run
```

Example:
```
Enter a public GitHub repository URL:
https://github.com/dotnet/runtime
Fetching files from runtime...
Found 152 files.
Generating YAML file...

=== Generated YAML ===
version: '1.0'
setup:
  name: runtime
  description: ".NET runtime setup script"
  prerequisites:
    - install: dotnet
  dependencies:
    dotnet:
      - "dotnet restore"
  run:
    - "dotnet build"