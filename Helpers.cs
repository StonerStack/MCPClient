using System.Text.Json;

namespace MCPClientApp
{
    public static class GeminiToolCallHelper
    {
        public record ToolCallInfo(string Json);
        public static ToolCallInfo? ParseGeminiToolCall(string llmResponse)
        {
            string? toolCodeBlock = null;
            var codeBlockStart = llmResponse.IndexOf("```tool_code");
            if (codeBlockStart != -1)
            {
                var codeBlockEnd = llmResponse.IndexOf("```", codeBlockStart + 1);
                if (codeBlockEnd != -1)
                {
                    toolCodeBlock = llmResponse.Substring(codeBlockStart + "```tool_code".Length, codeBlockEnd - (codeBlockStart + "```tool_code".Length)).Trim();
                }
            }
            string? extractedJson = null;
            if (!string.IsNullOrEmpty(toolCodeBlock))
            {
                if (llmResponse.TrimStart().StartsWith("{"))
                {
                    using var doc = JsonDocument.Parse(llmResponse);
                    var candidates = doc.RootElement.GetProperty("candidates");
                    if (candidates.GetArrayLength() > 0)
                    {
                        var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                        if (text != null)
                            extractedJson = text.Replace("```tool_code", "").Replace("```", "").Trim();
                    }
                }
                else
                {
                    extractedJson = toolCodeBlock.Replace("```tool_code", "").Replace("```", "").Trim();
                }
                if (extractedJson != null)
                {
                    int jsonStart = extractedJson.IndexOf('{');
                    int jsonEnd = extractedJson.LastIndexOf('}');
                    if (jsonStart != -1 && jsonEnd != -1 && jsonEnd > jsonStart)
                    {
                        extractedJson = extractedJson.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    }
                    return new ToolCallInfo(extractedJson);
                }
            }
            return null;
        }
    }
}
