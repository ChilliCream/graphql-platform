using System.Collections.Immutable;
using HotChocolate.Fusion.Errors;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Results;
using HotChocolate.Language;
using HotChocolate.Serialization;
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
    /// Determines whether the given schema is an Apollo Federation v2 subgraph
    /// based on the presence of an <c>@link</c> directive that imports the
    /// Apollo Federation specification.
    /// </summary>
    /// <param name="schema">
    /// The mutable schema definition to inspect.
    /// </param>
    /// <returns>
    /// <c>true</c> when the schema declares an <c>@link</c> directive whose
    /// <c>url</c> argument references the Apollo Federation specification;
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool IsFederationSchema(MutableSchemaDefinition schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        foreach (var directive in schema.Directives)
        {
            if (!directive.Name.Equals(FederationDirectiveNames.Link, StringComparison.Ordinal))
            {
                continue;
            }

            if (!directive.Arguments.TryGetValue("url", out var urlValue)
                || urlValue is not StringValueNode urlString)
            {
                continue;
            }

            if (urlString.Value.Contains(
                FederationSchemaAnalyzer.FederationUrlPrefix,
                StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

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

        var log = new CompositionLog();

        if (!FederationSchemaAnalyzer.Validate(schema, log))
        {
            return log
                .Select(entry => new CompositionError(entry.Message))
                .ToImmutableArray();
        }

        RemoveFederationInfrastructure.Apply(schema);
        GenerateLookupFields.Apply(schema);
        RewriteKeyDirectives.Apply(schema);
        TransformRequiresToRequire.Apply(schema);
        RemoveExternalFields.Apply(schema);
        StampConnectorKind.Apply(schema);

        return SchemaFormatter.FormatAsString(schema);
    }
}
