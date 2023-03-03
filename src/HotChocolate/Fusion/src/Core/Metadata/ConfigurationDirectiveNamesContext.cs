using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Metadata;

internal class ConfigurationDirectiveNamesContext : ISyntaxVisitorContext
{
    

    private ConfigurationDirectiveNamesContext(
        string variableDirective,
        string fetchDirective,
        string sourceDirective,
        string httpDirective,
        string fusionDirective)
    {
        VariableDirective = variableDirective;
        FetchDirective = fetchDirective;
        SourceDirective = sourceDirective;
        HttpDirective = httpDirective;
        FusionDirective = fusionDirective;
    }

    public string VariableDirective { get; }
    public string FetchDirective { get; }
    public string SourceDirective { get; }
    public string HttpDirective { get; }
    public string FusionDirective { get; }

    public bool IsConfigurationDirective(string name)
        => VariableDirective.EqualsOrdinal(name) ||
            FetchDirective.EqualsOrdinal(name) ||
            SourceDirective.EqualsOrdinal(name) ||
            HttpDirective.EqualsOrdinal(name) ||
            FusionDirective.EqualsOrdinal(name);

    public static ConfigurationDirectiveNamesContext Create(
        string? prefix = null,
        bool prefixSelf = false)
    {
        if (prefix is not null)
        {
            return new ConfigurationDirectiveNamesContext(
                $"{prefix}_{ConfigurationDirectiveNames.VariableDirective}",
                $"{prefix}_{ConfigurationDirectiveNames.ResolverDirective}",
                $"{prefix}_{ConfigurationDirectiveNames.SourceDirective}",
                $"{prefix}_{ConfigurationDirectiveNames.HttpDirective}",
                prefixSelf
                    ? $"{prefix}_{ConfigurationDirectiveNames.FusionDirective}"
                    : ConfigurationDirectiveNames.FusionDirective);
        }

        return new ConfigurationDirectiveNamesContext(
            ConfigurationDirectiveNames.VariableDirective,
            ConfigurationDirectiveNames.ResolverDirective,
            ConfigurationDirectiveNames.SourceDirective,
            ConfigurationDirectiveNames.HttpDirective,
            ConfigurationDirectiveNames.FusionDirective);
    }

    public static ConfigurationDirectiveNamesContext From(DocumentNode document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var schemaDef = document.Definitions.OfType<SchemaDefinitionNode>().FirstOrDefault();

        if (schemaDef is null)
        {
            // todo : exception
            throw new ArgumentException(
                "The provided document must at least contain a schema definition.",
                nameof(document));
        }

        if (TryGetPrefix(schemaDef.Directives, out var prefixSelf, out var prefix))
        {
            return new ConfigurationDirectiveNamesContext(
                $"{prefix}_{ConfigurationDirectiveNames.VariableDirective}",
                $"{prefix}_{ConfigurationDirectiveNames.ResolverDirective}",
                $"{prefix}_{ConfigurationDirectiveNames.SourceDirective}",
                $"{prefix}_{ConfigurationDirectiveNames.HttpDirective}",
                prefixSelf
                    ? $"{prefix}_{ConfigurationDirectiveNames.FusionDirective}"
                    : ConfigurationDirectiveNames.FusionDirective);
        }

        return new ConfigurationDirectiveNamesContext(
            ConfigurationDirectiveNames.VariableDirective,
            ConfigurationDirectiveNames.ResolverDirective,
            ConfigurationDirectiveNames.SourceDirective,
            ConfigurationDirectiveNames.HttpDirective,
            ConfigurationDirectiveNames.FusionDirective);
    }

    private static bool TryGetPrefix(
        IReadOnlyList<DirectiveNode> schemaDirectives,
        out bool prefixSelf,
        [NotNullWhen(true)] out string? prefix)
    {
        const string prefixedFusionDir = "_" + ConfigurationDirectiveNames.FusionDirective;

        foreach (var directive in schemaDirectives)
        {
            if (directive.Name.Value.EndsWith(prefixedFusionDir, StringComparison.Ordinal))
            {
                var prefixSelfArg =
                    directive.Arguments.FirstOrDefault(
                        t => t.Name.Value.EqualsOrdinal("prefixSelf"));

                if (prefixSelfArg?.Value is BooleanValueNode { Value: true })
                {
                    var prefixArg =
                        directive.Arguments.FirstOrDefault(
                            t => t.Name.Value.EqualsOrdinal("prefix"));

                    if (prefixArg?.Value is StringValueNode prefixVal &&
                        directive.Name.Value.EqualsOrdinal($"{prefixVal.Value}{prefixedFusionDir}"))
                    {
                        prefixSelf = true;
                        prefix = prefixVal.Value;
                        return true;
                    }
                }
            }
        }

        foreach (var directive in schemaDirectives)
        {
            if (directive.Name.Value.EqualsOrdinal(ConfigurationDirectiveNames.FusionDirective))
            {
                var prefixArg =
                    directive.Arguments.FirstOrDefault(
                        t => t.Name.Value.EqualsOrdinal("prefix"));

                if (prefixArg?.Value is StringValueNode prefixVal)
                {
                    prefixSelf = false;
                    prefix = prefixVal.Value;
                    return true;
                }
            }
        }

        prefixSelf = false;
        prefix = null;
        return false;
    }
}
