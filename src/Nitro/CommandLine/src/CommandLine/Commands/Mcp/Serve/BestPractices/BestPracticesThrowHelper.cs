using ModelContextProtocol;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices;

internal static class BestPracticesThrowHelper
{
    public static McpException BestPracticeNotFound(string id)
    {
        return new McpException($"Best practice '{id}' not found.");
    }
}
