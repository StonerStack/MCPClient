using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text.Json;

namespace MCPClientApp
{
    public static class MyMCPClient
    {
        public static async Task<IMcpClient> CreateMcpClient()
        {
            return await McpClientFactory.CreateAsync(
                new StdioClientTransport(new()
                {
                    Name = "MyFirstMCPClient",
                    Command = "dotnet run",
                    Arguments = ["--project", "E:\\Development\\MCPServerEcho"]
                })
            );
        }

        public static async Task PingMcpServer(IMcpClient mcpClient)
        {
            try
            {
                await mcpClient.PingAsync();
                Console.WriteLine("Successfully pinged the MCP Server");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to ping MCP Server: {ex.Message}");
            }
        }

        public static async Task<IList<McpClientTool>> ListMcpTools(IMcpClient mcpClient)
        {
            try
            {
                var tools = await mcpClient.ListToolsAsync();
                foreach (var tool in tools)
                {
                    Console.WriteLine($"Tool Name: {tool?.Name}, Description: {tool?.Description}, Schema: {tool?.ReturnJsonSchema}");
                }
                return tools;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to list tools: {ex.Message}");
                return Array.Empty<McpClientTool>();
            }
        }

        public static async Task DemoDirectToolCall(IMcpClient mcpClient, IList<McpClientTool> availableTools)
        {
            if (availableTools.Count < 3) return;
            string? toolName = availableTools[1].Name;
            var toolParameters = new Dictionary<string, object?>();
            Console.WriteLine($"Calling tool: {toolName}");
            if (toolName != null && toolName.Contains("Echo", StringComparison.InvariantCultureIgnoreCase))
                toolParameters["message"] = "This is the Echo Tool.";
            else if (toolName != null && toolName.Contains("add", StringComparison.CurrentCultureIgnoreCase))
            {
                toolParameters["name"] = "Stoner Stack HR";
                toolParameters["id"] = 1213;
                toolParameters["email"] = "stonerHR@gmail.com";
                toolParameters["department"] = "HR";
            }
            var result = await mcpClient.CallToolAsync(toolName ?? string.Empty, toolParameters);
            PrintToolResult(result);
        }

        public static async Task DemoLlmToolCall(IMcpClient mcpClient, IList<McpClientTool> availableTools , string userPrompt)
        {
           
            // Format each tool with its name, description, and schema in a readable format
            var formattedTools = availableTools.Select(t => 
            {
                var schemaJson = JsonSerializer.Serialize(t.JsonSchema, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                return $"Tool: {t.Name}\nDescription: {t.Description}\nSchema: {schemaJson}";
            }).ToList(); // Materialize the query for debugging

            //Formulate the prompt for LLM
            string toolsForPrompt = string.Join("\n\n", formattedTools) +
                           "\n\nIf you need to use any tools, provide your response in tool_code format.";
            string prompt = userPrompt + toolsForPrompt;

            // Debug: Print final prompt
            Console.WriteLine(prompt);
            Console.WriteLine("\n\n**********************************************************************************\n\n");
           

            string llmResponse = await GeminiClient.CallGeminiAsync(prompt);
            
            Console.WriteLine("Gemini LLM Response:");
            Console.WriteLine(llmResponse);
            Console.WriteLine("\n\n**********************************************************************************\n\n");
            
            var toolCallInfo = GeminiToolCallHelper.ParseGeminiToolCall(llmResponse);
            //Add a while loop if multiple tool calls are expected.
            if (toolCallInfo != null) // If Tool call is required then we are going to call the MCP tool from over server.
            {
                try
                {
                    var toolCall = JsonDocument.Parse(toolCallInfo.Json);
                    var root = toolCall.RootElement;
                    string? parsedToolName = root.GetProperty("tool").GetString();
                    var toolParamsDict = new Dictionary<string, object?>();

                    // Handle case where tool might not have parameters
                    if (root.TryGetProperty("parameters", out var parameters))
                    {
                        foreach (var prop in parameters.EnumerateObject())
                        {
                            toolParamsDict[prop.Name] = prop.Value.ToString();
                        }
                    }

                    Console.WriteLine("\n\n**********************************************************************************\n\n");
                    Console.WriteLine($"Calling MCP tool: {parsedToolName} with parameters: {string.Join(", ", toolParamsDict.Select(kv => kv.Key + ": " + kv.Value))}");

                    var mcpResult = await mcpClient.CallToolAsync(parsedToolName ?? string.Empty, toolParamsDict);
                    
                                        PrintToolResult(mcpResult);
                                        Console.WriteLine("\n\n**********************************************************************************\n\n");
                    
                    // Send MCP tool output to LLM for further use
                    var toolOutput = mcpResult.Content != null ? string.Join("\n", mcpResult.Content.Select(cb => cb.ToAIContent())) : string.Empty;
                    if (mcpResult.StructuredContent != null)
                        toolOutput += "\nStructured Content: " + mcpResult.StructuredContent;

                    if (toolOutput.Length == 0)
                        toolOutput = "SUCCESS";

                    string followUpPrompt = $"Tool output: {toolOutput}. Original User Query: {userPrompt}.\n Process Tool Output and give appropriate response.";
                    string llmFollowUpResponse = await GeminiClient.CallGeminiAsync(followUpPrompt);
                    Console.WriteLine("Gemini LLM Follow-up Response:");
                    Console.WriteLine(llmFollowUpResponse);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to parse tool call from Gemini response: {ex.Message}");
                }
            }
        }

        public static void PrintToolResult(CallToolResult result)
        {
            if (result == null)
            {
                Console.WriteLine("No result returned from tool call.");
                return;
            }
            if (result?.IsError == true)
                Console.WriteLine("Tool call resulted in an error.");
            else
            {
                if (result.Content != null)
                {
                    foreach (var contentBlock in result.Content)
                        Console.WriteLine($"Content Type: {contentBlock.Type}, Content: {contentBlock.ToAIContent()}");
                }
                if (result.StructuredContent != null)
                    Console.WriteLine($"Structured Content: {result.StructuredContent}");
            }
        }
    }
}
