using System.Collections.Immutable;
using HotChocolate.Fusion.Errors;
using HotChocolate.Fusion.Results;
using HotChocolate.Language;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;
using static HotChocolate.Fusion.ApolloFederation.Properties.FederationResources;

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

        MutableSchemaDefinition schema;

        try
        {
            schema = SchemaParser.Parse(federationSdl);
        }
        catch (SyntaxException ex)
        {
            return new CompositionError(
                string.Format(FederationSchemaTransformer_ParseFailed, ex.Message));
        }

        var errors = FederationSchemaAnalyzer.Validate(schema);

        if (errors.Count > 0)
        {
            return errors.ToImmutableArray();
        }

        RemoveFederationInfrastructure.Apply(schema);
        GenerateLookupFields.Apply(schema);
        RewriteKeyDirectives.Apply(schema);
        TransformRequiresToRequire.Apply(schema);
        RemoveExternalFields.Apply(schema);

        return SchemaFormatter.FormatAsString(schema);
    }
}
