using HotChocolate.Fusion.Errors;
using HotChocolate.Language;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.ApolloFederation.Properties.FederationResources;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Validates a <see cref="MutableSchemaDefinition"/> for Apollo Federation v2 compatibility
/// and detects unsupported directives.
/// </summary>
internal static class FederationSchemaAnalyzer
{
    private const string FederationUrlPrefix = "specs.apollo.dev/federation";

    private static readonly HashSet<string> s_unsupportedDirectives =
    [
        FederationDirectiveNames.ComposeDirective,
        FederationDirectiveNames.Authenticated,
        FederationDirectiveNames.RequiresScopes,
        FederationDirectiveNames.Policy,
        FederationDirectiveNames.InterfaceObject
    ];

    /// <summary>
    /// Validates the given federation schema and returns any composition errors.
    /// </summary>
    /// <param name="schema">
    /// The mutable schema definition to validate.
    /// </param>
    /// <returns>
    /// A list of <see cref="CompositionError"/> instances. An empty list indicates success.
    /// </returns>
    public static List<CompositionError> Validate(MutableSchemaDefinition schema)
    {
        var errors = new List<CompositionError>();

        ValidateFederationVersion(schema, errors);
        ValidateUnsupportedDirectives(schema, errors);

        return errors;
    }

    private static void ValidateFederationVersion(
        MutableSchemaDefinition schema,
        List<CompositionError> errors)
    {
        var federationVersion = FindFederationVersion(schema);

        if (federationVersion is null)
        {
            errors.Add(new CompositionError(FederationSchemaAnalyzer_FederationV1NotSupported));
        }
    }

    private static string? FindFederationVersion(MutableSchemaDefinition schema)
    {
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

            var url = urlString.Value;

            if (!url.Contains(FederationUrlPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            // Extract version from URL like
            // "https://specs.apollo.dev/federation/v2.5"
            var lastSlash = url.LastIndexOf('/');

            if (lastSlash >= 0 && lastSlash < url.Length - 1)
            {
                return url[(lastSlash + 1)..];
            }
        }

        return null;
    }

    private static void ValidateUnsupportedDirectives(
        MutableSchemaDefinition schema,
        List<CompositionError> errors)
    {
        foreach (var name in s_unsupportedDirectives)
        {
            if (schema.DirectiveDefinitions.ContainsName(name))
            {
                errors.Add(new CompositionError(string.Format(
                    FederationSchemaAnalyzer_DirectiveNotSupported,
                    name)));
            }
        }
    }
}
