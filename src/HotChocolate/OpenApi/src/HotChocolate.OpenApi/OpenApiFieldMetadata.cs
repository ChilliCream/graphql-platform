using System.Text.Json;
using HotChocolate.Resolvers;

namespace HotChocolate.OpenApi;

/// <summary>
/// Skimmed Field Metadata
/// </summary>
internal sealed class OpenApiFieldMetadata
{
    public bool IsErrorsField { get; set; }

    public string? PropertyName { get; set; }

    public string? InputFieldName { get; set; }

    public bool UseParentResult { get; set; }

    public Func<IResolverContext, Task<JsonElement>>? Resolver { get; set; }
}
