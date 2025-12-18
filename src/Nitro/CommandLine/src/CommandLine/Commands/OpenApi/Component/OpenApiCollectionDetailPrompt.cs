using ChilliCream.Nitro.CommandLine.Client;

namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi.Components;

internal sealed class OpenApiCollectionDetailPrompt
{
    private readonly IOpenApiCollectionDetailPrompt_OpenApiCollection _data;

    private OpenApiCollectionDetailPrompt(IOpenApiCollectionDetailPrompt_OpenApiCollection data)
    {
        _data = data;
    }

    public OpenApiCollectionDetailPromptResult ToObject(string[] formats)
    {
        return new OpenApiCollectionDetailPromptResult
        {
            Id = _data.Id,
            Name = _data.Name
        };
    }

    public static OpenApiCollectionDetailPrompt From(IOpenApiCollectionDetailPrompt_OpenApiCollection data) => new(data);

    public class OpenApiCollectionDetailPromptResult
    {
        public required string Id { get; init; }

        public required string Name { get; init; }
    }
}
