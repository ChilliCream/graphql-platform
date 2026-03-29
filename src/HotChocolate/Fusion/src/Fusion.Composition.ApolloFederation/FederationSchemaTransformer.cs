using System.Collections.Immutable;
using HotChocolate.Fusion.Errors;
using HotChocolate.Fusion.Results;
using HotChocolate.Language;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Transforms an Apollo Federation v2 subgraph SDL into a Composite Schema Spec
/// source schema SDL suitable for the HotChocolate Fusion composition pipeline.
/// </summary>
public static class FederationSchemaTransformer
{
    /// <summary>
    /// Transforms the given Apollo Federation v2 subgraph SDL.
    /// </summary>
    /// <param name="federationSdl">
    /// The Apollo Federation v2 subgraph SDL to transform.
    /// </param>
    /// <returns>
    /// A <see cref="CompositionResult{TValue}"/> containing the transformed SDL string
    /// on success, or composition errors on failure.
    /// </returns>
    public static CompositionResult<string> Transform(string federationSdl)
    {
        ArgumentException.ThrowIfNullOrEmpty(federationSdl);

        DocumentNode document;

        try
        {
            document = Utf8GraphQLParser.Parse(federationSdl);
        }
        catch (SyntaxException ex)
        {
            return new CompositionError(
                $"Failed to parse federation SDL: {ex.Message}");
        }

        var analysis = FederationSchemaAnalyzer.Analyze(document);

        if (analysis.HasErrors)
        {
            return analysis.Errors.ToImmutableArray();
        }

        document = RemoveFederationInfrastructure.Apply(document, analysis);
        document = RewriteKeyDirectives.Apply(document);
        document = GenerateLookupFields.Apply(document, analysis);
        document = TransformRequiresToRequire.Apply(document, analysis);

        return document.ToString(indented: true);
    }

    /// <summary>
    /// Transforms the given Apollo Federation v2 subgraph SDL into a
    /// <see cref="SourceSchemaText"/> for the composition pipeline.
    /// </summary>
    /// <param name="schemaName">
    /// The name to assign to the source schema.
    /// </param>
    /// <param name="federationSdl">
    /// The Apollo Federation v2 subgraph SDL to transform.
    /// </param>
    /// <returns>
    /// A <see cref="CompositionResult{TValue}"/> containing a <see cref="SourceSchemaText"/>
    /// on success, or composition errors on failure.
    /// </returns>
    public static CompositionResult<SourceSchemaText> TransformToSourceSchema(
        string schemaName,
        string federationSdl)
    {
        ArgumentException.ThrowIfNullOrEmpty(schemaName);

        var (_, isFailure, sdl, errors) = Transform(federationSdl);

        if (isFailure)
        {
            return errors;
        }

        return new SourceSchemaText(schemaName, sdl);
    }
}
