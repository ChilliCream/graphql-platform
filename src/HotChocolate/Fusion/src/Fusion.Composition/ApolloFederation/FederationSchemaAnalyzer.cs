using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Logging.Contracts;
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
    internal const string FederationUrlPrefix = "specs.apollo.dev/federation";

    private static readonly HashSet<string> s_unsupportedDirectives =
    [
        FederationDirectiveNames.ComposeDirective,
        FederationDirectiveNames.Authenticated,
        FederationDirectiveNames.RequiresScopes,
        FederationDirectiveNames.Policy,
        FederationDirectiveNames.InterfaceObject
    ];

    /// <summary>
    /// Validates the given federation schema and writes any composition errors to the
    /// provided <paramref name="log"/>.
    /// </summary>
    /// <param name="schema">
    /// The mutable schema definition to validate.
    /// </param>
    /// <param name="log">
    /// The composition log that receives validation errors.
    /// </param>
    /// <returns>
    /// <c>true</c> when validation passed (no errors were written); otherwise, <c>false</c>.
    /// </returns>
    public static bool Validate(MutableSchemaDefinition schema, ICompositionLog log)
    {
        var entryCountBefore = log.Count();

        ValidateFederationVersion(schema, log);
        ValidateUnsupportedDirectives(schema, log);

        // Pre-existing entries on the log are not ours to report on; this run reports
        // failure only when it wrote new entries (all of which are errors) of its own.
        return log.Count() == entryCountBefore;
    }

    private static void ValidateFederationVersion(
        MutableSchemaDefinition schema,
        ICompositionLog log)
    {
        var federationVersion = FindFederationVersion(schema);

        if (federationVersion is null)
        {
            log.Write(
                LogEntryBuilder.New()
                    .SetMessage(FederationSchemaAnalyzer_FederationV1NotSupported)
                    .SetCode(LogEntryCodes.FederationV1NotSupported)
                    .SetSeverity(LogSeverity.Error)
                    .SetSchema(schema)
                    .Build());
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
        ICompositionLog log)
    {
        foreach (var name in s_unsupportedDirectives)
        {
            if (schema.DirectiveDefinitions.ContainsName(name))
            {
                log.Write(
                    LogEntryBuilder.New()
                        .SetMessage(
                            FederationSchemaAnalyzer_DirectiveNotSupported,
                            name)
                        .SetCode(LogEntryCodes.FederationDirectiveNotSupported)
                        .SetSeverity(LogSeverity.Error)
                        .SetSchema(schema)
                        .Build());
            }
        }
    }
}
