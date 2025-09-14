using ModelContextProtocol;
using MCPClientApp;
namespace MCPClientApp
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var mcpClient = await MyMCPClient.CreateMcpClient();
            await MyMCPClient.PingMcpServer(mcpClient);
            var availableTools = await MyMCPClient.ListMcpTools(mcpClient);
            if (availableTools.Count == 0) return;

            // Step 1: Call a tool directly (demo)
            await MyMCPClient.DemoDirectToolCall(mcpClient, availableTools);

            //Step 2: Interact with LLM and handle tool calls
            string userPrompt = "Add a new Employee named as Henry with ID 12345 in the the deparment of Sales with email as henry.sales@example.com";
            await MyMCPClient.DemoLlmToolCall(mcpClient, availableTools, userPrompt);
        }
    }
}
