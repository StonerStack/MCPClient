using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public static class AnthropicClient
{
    public static async Task<string> CallClaudeAsync(string prompt)
    {
        // Check for .env in bin directory and project root
        string binEnvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
        string projectRootEnvPath = Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName, ".env");
        string envPath = null;
        if (File.Exists(binEnvPath))
        {
            envPath = binEnvPath;
        }
        else if (File.Exists(projectRootEnvPath))
        {
            envPath = projectRootEnvPath;
        }
        if (envPath == null)
        {
            return $"No .env file found in either {binEnvPath} or {projectRootEnvPath}";
        }
        string apiKey = null;
        foreach (var line in File.ReadAllLines(envPath))
        {
            if (line.StartsWith("ANTHROPIC_API_KEY="))
            {
                apiKey = line.Substring("ANTHROPIC_API_KEY=".Length).Trim();
                break;
            }
        }
        if (string.IsNullOrEmpty(apiKey))
        {
            return "Anthropic API key not found in .env file.";
        }
        using var httpClient = new HttpClient();
        var url = "https://api.anthropic.com/v1/messages";
        httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        var payload = new
        {
            model = "claude-3-5-sonnet-20241022", // You can change to claude-3-sonnet-202406204", // You can change to claude-3-sonnet or haiku if needed
            max_tokens = 1024,
            messages = new[]
            {
                new {
                    role = "user",
                    content = prompt
                }
            }
        };
        var jsonPayload = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        System.Console.WriteLine(jsonPayload);
        var response = await httpClient.PostAsync(url, content);
        var responseBody = await response.Content.ReadAsStringAsync();
        return responseBody;
    }
}
