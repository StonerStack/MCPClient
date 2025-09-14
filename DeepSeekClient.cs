using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public static class DeepSeekClient
{
    public static async Task<string> CallDeepSeekAsync(string prompt)
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
        var payload = new
        {
            model = "deepseek/deepseek-chat-v3.1:free",
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
        Console.WriteLine(jsonPayload);
        var response = await httpClient.PostAsync(url, content);
        var responseBody = await response.Content.ReadAsStringAsync();
        return responseBody;
    }
}
