using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public static class OpenAIClient
{
    public static async Task<string> CallOpenRouterOpenAIAsync(string prompt, string referer = null, string title = null)
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
            if (line.StartsWith("OPENROUTER_API_KEY="))
            {
                apiKey = line.Substring("OPENROUTER_API_KEY=".Length).Trim();
                break;
            }
        }
        if (string.IsNullOrEmpty(apiKey))
        {
            return "OpenRouter API key not found in .env file.";
        }
        using var httpClient = new HttpClient();
        var url = "https://openrouter.ai/api/v1/chat/completions";
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        if (!string.IsNullOrEmpty(referer))
            httpClient.DefaultRequestHeaders.Add("HTTP-Referer", referer);
        if (!string.IsNullOrEmpty(title))
            httpClient.DefaultRequestHeaders.Add("X-Title", title);
        var payload = new
        {
            model = "openai/gpt-oss-20b:free",
            messages = new[]
            {
                new {
                    role = "user",
                    content = prompt
                }
            },
            response_format = new { type = "json_object" }
        };
        var jsonPayload = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        Console.WriteLine(jsonPayload);
        var response = await httpClient.PostAsync(url, content);
        var responseBody = await response.Content.ReadAsStringAsync();
        return responseBody;
    }
}
