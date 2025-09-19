using HotChocolate.Features;
using HotChocolate.Fusion.Execution;
using HotChocolate.Language;

// ReSharper disable once CheckNamespace
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HotChocolate.Execution;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides extension methods for <see cref="ISchemaDefinition"/> to access
/// </summary>
public static class FusionSchemaDefinitionExtensions
{
    public static FusionRequestOptions GetRequestOptions(
        this ISchemaDefinition schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        return schema.Features.GetRequired<FusionRequestOptions>();
    }

    public static ParserOptions GetParserOptions(
        this ISchemaDefinition schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        return schema.Features.GetRequired<ParserOptions>();
    }
}
