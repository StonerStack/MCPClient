using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public static class GeminiClient
{
    public static async Task<string> CallGeminiAsync(string prompt)
    {
        // Check for .env in bin directory and project root

        #region GET API KEY
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
            if (line.StartsWith("GEMINI_API_KEY="))
            {
                apiKey = line.Substring("GEMINI_API_KEY=".Length).Trim();
                break;
            }
        }
        if (string.IsNullOrEmpty(apiKey))
        {
            return "Gemini API key not found in .env file.";
        }

        #endregion
        
        using var httpClient = new HttpClient();
        var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-exp:generateContent";
        httpClient.DefaultRequestHeaders.Add("X-goog-api-key", apiKey);
        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    },
                    role = "user"
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
