# RepoAnalyzer

RepoAnalyzer is a .NET console application that analyzes a public GitHub repository by retrieving all files and leveraging **Azure OpenAI** to provide insights. It helps developers understand the structure, dependencies, and purpose of a repository.

## Features
- Fetches all files in a given public GitHub repository.
- Extracts relevant content from key files (`.cs`, `.js`, `.md`, etc.).
- Uses **Azure OpenAI** (GPT-4 Turbo) to generate a summary of the repository.
- Provides insights into the project's structure and key technologies.

## Getting Started

### Prerequisites
- **.NET 6 or later** installed ([Download](https://dotnet.microsoft.com/en-us/download))
- **GitHub API access** (public repos only)
- **Azure OpenAI API Key**
