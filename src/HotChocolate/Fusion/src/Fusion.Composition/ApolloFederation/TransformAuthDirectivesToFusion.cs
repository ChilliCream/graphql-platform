using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Rewrites the Apollo Federation <c>@authenticated</c>, <c>@requiresScopes</c> and
/// <c>@policy</c> directives, including renamed and namespaced spellings resolved through
/// <c>@link</c>, into the Fusion source schema directives with <c>[[String!]!]</c> arguments.
/// </summary>
internal static class TransformAuthDirectivesToFusion
{
    private const string DefaultNamespace = "federation";

    private static readonly string[] s_directiveNames =
    [
        FederationDirectiveNames.Authenticated,
        FederationDirectiveNames.RequiresScopes,
        FederationDirectiveNames.Policy
    ];

    private static readonly string[] s_scalarNames =
    [
        FederationTypeNames.Scope,
        FederationTypeNames.Policy
    ];

    /// <summary>
    /// Applies the authorization directive transformation on the schema.
    /// </summary>
    /// <param name="schema">
    /// The mutable schema definition to transform in place.
    /// </param>
    public static void Apply(MutableSchemaDefinition schema)
    {
        var vocabulary = Vocabulary.Resolve(schema);

        foreach (var type in schema.Types)
        {
            switch (type)
            {
                case MutableComplexTypeDefinition complexType:
                    Rewrite(complexType.Directives, vocabulary);

                    foreach (var field in complexType.Fields)
                    {
                        Rewrite(field.Directives, vocabulary);
                    }

                    break;

                case MutableEnumTypeDefinition enumType:
                    Rewrite(enumType.Directives, vocabulary);
                    break;

                case MutableScalarTypeDefinition scalarType:
                    Rewrite(scalarType.Directives, vocabulary);
                    break;
            }
        }

        RemoveApolloDefinitions(schema, vocabulary);
    }

    private static void Rewrite(DirectiveCollection directives, Vocabulary vocabulary)
    {
        if (directives.Count == 0)
        {
            return;
        }

        foreach (var directive in directives.AsEnumerable().ToList())
        {
            if (!vocabulary.TryGetFusionName(directive.Name, out var fusionName))
            {
                continue;
            }

            var definition = FusionBuiltIns.SourceSchemaDirectives[fusionName];

            directives.Replace(
                directive,
                fusionName switch
                {
                    FederationDirectiveNames.RequiresScopes => CreateGroupedDirective(
                        definition,
                        directive,
                        WellKnownArgumentNames.Scopes),
                    FederationDirectiveNames.Policy => CreateGroupedDirective(
                        definition,
                        directive,
                        WellKnownArgumentNames.Policies),
                    _ => new Directive(definition)
                });
        }
    }

    private static Directive CreateGroupedDirective(
        MutableDirectiveDefinition definition,
        Directive source,
        string argumentName)
    {
        // A missing argument is left missing, the source schema validation reports it.
        return source.Arguments.TryGetValue(argumentName, out var value)
            ? new Directive(
                definition,
                new ArgumentAssignment(argumentName, ToGroups(value)))
            : new Directive(definition);
    }

    // GraphQL input coercion permits a single value wherever a list is expected. Values that
    // cannot be read as strings are kept as written, the source schema validation reports them.
    private static IValueNode ToGroups(IValueNode value)
    {
        return value switch
        {
            StringValueNode => new ListValueNode(new ListValueNode(value)),
            ListValueNode list => ToGroups(list) ?? value,
            _ => value
        };
    }

    private static ListValueNode? ToGroups(ListValueNode list)
    {
        var groups = new List<IValueNode>(list.Items.Count);

        foreach (var item in list.Items)
        {
            switch (item)
            {
                case StringValueNode:
                    groups.Add(new ListValueNode(item));
                    break;

                case ListValueNode group when group.Items.All(static i => i is StringValueNode):
                    groups.Add(group);
                    break;

                default:
                    return null;
            }
        }

        return new ListValueNode(groups);
    }

    private static void RemoveApolloDefinitions(
        MutableSchemaDefinition schema,
        Vocabulary vocabulary)
    {
        // The parser keeps the Fusion definition when a subgraph declares a directive under a
        // Fusion name, so only definitions that are not the Fusion ones are Apollo's.
        foreach (var name in vocabulary.DirectiveNames)
        {
            if (schema.DirectiveDefinitions.TryGetDirective(name, out var definition)
                && !(FusionBuiltIns.SourceSchemaDirectives.TryGetValue(name, out var builtIn)
                    && ReferenceEquals(definition, builtIn)))
            {
                schema.DirectiveDefinitions.Remove(name);
            }
        }

        var referencedTypeNames = RemoveFederationInfrastructure.CollectReferencedTypeNames(schema);

        foreach (var name in vocabulary.ScalarNames)
        {
            if (!referencedTypeNames.Contains(name)
                && schema.Types.TryGetType<MutableScalarTypeDefinition>(name, out _))
            {
                schema.Types.Remove(name);
            }
        }
    }

    /// <summary>
    /// The names under which a subgraph spells the Apollo authorization directives and scalars.
    /// </summary>
    private sealed class Vocabulary
    {
        private readonly Dictionary<string, string> _fusionNames = new(StringComparer.Ordinal);
        private readonly HashSet<string> _scalarNames = new(StringComparer.Ordinal);

        public IEnumerable<string> DirectiveNames => _fusionNames.Keys;

        public IEnumerable<string> ScalarNames => _scalarNames;

        public bool TryGetFusionName(string name, out string fusionName)
        {
            // A name that is not remapped by an import already is the Fusion directive.
            if (_fusionNames.TryGetValue(name, out fusionName!))
            {
                return true;
            }

            fusionName = name;
            return Array.IndexOf(s_directiveNames, name) >= 0;
        }

        public static Vocabulary Resolve(MutableSchemaDefinition schema)
        {
            var vocabulary = new Vocabulary();

            // The unqualified names are Fusion's own, they are only listed so that their Apollo
            // definitions are removed.
            foreach (var name in s_directiveNames)
            {
                vocabulary._fusionNames[name] = name;
            }

            foreach (var link in schema.Directives[FederationDirectiveNames.Link])
            {
                if (!link.Arguments.TryGetValue("url", out var urlValue)
                    || urlValue is not StringValueNode url
                    || !url.Value.Contains(
                        FederationSchemaAnalyzer.FederationUrlPrefix,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                var ns = link.Arguments.TryGetValue("as", out var asValue)
                    && asValue is StringValueNode { Value.Length: > 0 } alias
                        ? alias.Value
                        : DefaultNamespace;

                vocabulary.AddNamespaced(ns);

                if (link.Arguments.TryGetValue("import", out var importValue)
                    && importValue is ListValueNode imports)
                {
                    foreach (var import in imports.Items)
                    {
                        vocabulary.AddImport(import);
                    }
                }
            }

            return vocabulary;
        }

        private void AddNamespaced(string ns)
        {
            foreach (var name in s_directiveNames)
            {
                _fusionNames[$"{ns}__{name}"] = name;
            }

            foreach (var name in s_scalarNames)
            {
                _scalarNames.Add($"{ns}__{name}");
            }
        }

        private void AddImport(IValueNode import)
        {
            string? name = null;
            string? alias = null;

            switch (import)
            {
                case StringValueNode importName:
                    name = importName.Value;
                    break;

                case ObjectValueNode importObject:
                    foreach (var field in importObject.Fields)
                    {
                        if (field.Value is not StringValueNode fieldValue)
                        {
                            continue;
                        }

                        switch (field.Name.Value)
                        {
                            case "name":
                                name = fieldValue.Value;
                                break;

                            case "as":
                                alias = fieldValue.Value;
                                break;
                        }
                    }

                    break;
            }

            if (name is null)
            {
                return;
            }

            if (name.StartsWith('@'))
            {
                var directiveName = name[1..];

                if (Array.IndexOf(s_directiveNames, directiveName) >= 0)
                {
                    _fusionNames[(alias ?? name).TrimStart('@')] = directiveName;
                }

                return;
            }

            if (Array.IndexOf(s_scalarNames, name) >= 0)
            {
                _scalarNames.Add(alias ?? name);
            }
        }
    }
}
