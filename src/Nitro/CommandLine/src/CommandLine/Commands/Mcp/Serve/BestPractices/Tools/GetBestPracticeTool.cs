using System.ComponentModel;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Extensions;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Models;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Services;
using ModelContextProtocol.Server;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Tools;

[McpServerToolType]
internal static class GetBestPracticeTool
{
    [McpServerTool(Name = "get_best_practice")]
    [Description(
        "Retrieve the full content of a Hot Chocolate best practice document by its identifier. "
            + "Always call search_best_practices first to discover relevant document IDs, "
            + "then call this tool to get the full implementation guide including code examples.")]
    public static string Get(
        [Description("The identifier of the best practice document, as returned by search_best_practices.")] string id)
    {
        var repository = BestPracticeRepositoryHolder.Instance;

        var doc = repository.GetById(id) ?? throw BestPracticesThrowHelper.BestPracticeNotFound(id);

        var result = new BestPracticeGetResult
        {
            Id = doc.Id,
            Title = doc.Title,
            Category = doc.Category.ToDisplayString(),
            Tags = doc.Tags,
            Abstract = doc.Abstract,
            Content = doc.Body
        };

        return JsonSerializer.Serialize(result, BestPracticeJsonContext.Default.BestPracticeGetResult);
    }
}
