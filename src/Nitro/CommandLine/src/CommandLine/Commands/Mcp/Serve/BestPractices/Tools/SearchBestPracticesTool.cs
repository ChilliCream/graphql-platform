using System.ComponentModel;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Models;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Services;
using ModelContextProtocol.Server;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Tools;

[McpServerToolType]
internal static class SearchBestPracticesTool
{
    [McpServerTool(Name = "search_best_practices")]
    [Description(
        "Search the Hot Chocolate best practices knowledge base. "
            + "Use this tool to discover relevant best practice documents before implementing any feature. "
            + "It returns a list of document summaries - use get_best_practice to retrieve the full content of any document.")]
    public static string Search(
        [Description("Filter by category.")] BestPracticeCategory? category = null,
        [Description(
            "Filter by tags (AND semantics - document must have ALL specified tags). "
                + "Valid tags: ddd, clean-architecture, graphql-first, service-layer, mediator, "
                + "rapid-development, relay, hot-chocolate-16")]
            string[]? tags = null,
        [Description("Free-text search across title, abstract, and body.")] string? query = null)
    {
        var repository = BestPracticeRepositoryHolder.Instance;

        var request = new BestPracticeSearchRequest
        {
            Category = category,
            Tags = tags,
            Query = query
        };

        var results = repository.Search(request, projectStyles: null);

        var response = new BestPracticeSearchResponse { Results = results, TotalCount = results.Count };

        return JsonSerializer.Serialize(response, BestPracticeJsonContext.Default.BestPracticeSearchResponse);
    }
}
