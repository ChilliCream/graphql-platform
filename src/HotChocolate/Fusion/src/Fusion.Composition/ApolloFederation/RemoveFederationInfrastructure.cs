using HotChocolate.Fusion.Definitions;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Removes Apollo Federation infrastructure types, directives, and fields
/// from a mutable schema definition.
/// </summary>
internal static class RemoveFederationInfrastructure
{
    // Apollo's policy sub-spec is linked separately from the main federation spec, e.g.
    // @link(url: "https://specs.apollo.dev/policy/v0.1", import: ["@policy"]).
    private const string PolicySpecUrlPrefix = "specs.apollo.dev/policy";
    private const string PoliciesArgumentName = "policies";
    private const string ScopesArgumentName = "scopes";

    private static readonly HashSet<string> s_federationDirectiveNames =
    [
        with(StringComparer.Ordinal),
        FederationDirectiveNames.Key,
        FederationDirectiveNames.Requires,
        FederationDirectiveNames.Provides,
        FederationDirectiveNames.External,
        FederationDirectiveNames.Extends,
        FederationDirectiveNames.Link,
        FederationDirectiveNames.Shareable,
        FederationDirectiveNames.Inaccessible,
        FederationDirectiveNames.Override,
        FederationDirectiveNames.Tag,
        FederationDirectiveNames.ComposeDirective
    ];

    private static readonly HashSet<string> s_federationScalarNames =
    [
        with(StringComparer.Ordinal),
        FederationTypeNames.Any,
        FederationTypeNames.FieldSet,
        FederationTypeNames.LegacyFieldSet,
        FederationTypeNames.Policy,
        FederationTypeNames.Scope
    ];

    /// <summary>
    /// Applies the transformation to remove federation infrastructure from the schema.
    /// </summary>
    /// <param name="schema">
    /// The mutable schema definition to transform in place.
    /// </param>
    public static void Apply(MutableSchemaDefinition schema)
    {
        // Both translations below may need to install Fusion's canonical @policy directive
        // definition. It is tracked here, across both calls, so a source schema that uses more
        // than one of these directives shares a single instance instead of colliding on the name
        // (Apollo's own @policy, unaliased, is defined under that same name until translated).
        MutableDirectiveDefinition? fusionPolicyDefinition = null;

        // Rewrite Apollo's @policy applications into Fusion's @policy(names:) shape before the
        // Apollo directive definitions are dropped below, so the authorization semantic survives
        // the import instead of being silently discarded.
        TranslatePolicyDirective(schema, ref fusionPolicyDefinition);

        // Rewrite Apollo's @authenticated and @requiresScopes applications into Fusion's
        // @policy(names:) shape for the same reason.
        TranslateAuthDirectives(schema, ref fusionPolicyDefinition);

        // Remove federation directive definitions. @policy, @authenticated, and @requiresScopes
        // are removed by their own translation above (which resolves their possibly-aliased local
        // name), not here.
        foreach (var name in s_federationDirectiveNames)
        {
            schema.DirectiveDefinitions.Remove(name);
        }

        // Remove _entities and _service fields from query type.
        if (schema.QueryType is not null)
        {
            schema.QueryType.Fields.Remove(FederationFieldNames.Entities);
            schema.QueryType.Fields.Remove(FederationFieldNames.Service);
        }

        // Remove @link directives from schema.
        foreach (var directive in schema.Directives[FederationDirectiveNames.Link].ToList())
        {
            schema.Directives.Remove(directive);
        }

        // Federation v1's @extends marker is represented by the source schema's type
        // contribution after preprocessing and has no Composite Schema equivalent.
        foreach (var type in schema.Types.OfType<MutableComplexTypeDefinition>())
        {
            foreach (var directive in type.Directives[FederationDirectiveNames.Extends].ToList())
            {
                type.Directives.Remove(directive);
            }
        }

        // Collect the type names still referenced after the federation directives and fields
        // above have been removed. Federation types such as FieldSet are exported vocabulary and
        // may be used by user-defined members (for example as a custom directive argument type).
        // Once referenced by a surviving member the type is part of the user's schema, so removing
        // it here would leave a dangling reference; those types are kept while unreferenced
        // federation types continue to be stripped.
        var referencedTypeNames = CollectReferencedTypeNames(schema);

        // Remove federation scalar types that are no longer referenced.
        foreach (var name in s_federationScalarNames)
        {
            if (!referencedTypeNames.Contains(name))
            {
                schema.Types.Remove(name);
            }
        }

        // Remove the _Service type and _Entity union when no longer referenced.
        if (!referencedTypeNames.Contains(FederationTypeNames.Service))
        {
            schema.Types.Remove(FederationTypeNames.Service);
        }

        if (!referencedTypeNames.Contains(FederationTypeNames.Entity))
        {
            schema.Types.Remove(FederationTypeNames.Entity);
        }
    }

    private static HashSet<string> CollectReferencedTypeNames(MutableSchemaDefinition schema)
    {
        var referencedTypeNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var type in schema.Types)
        {
            switch (type)
            {
                case MutableComplexTypeDefinition complexType:
                    foreach (var field in complexType.Fields)
                    {
                        referencedTypeNames.Add(field.Type.NamedType().Name);

                        foreach (var argument in field.Arguments)
                        {
                            referencedTypeNames.Add(argument.Type.NamedType().Name);
                        }
                    }

                    break;

                case MutableInputObjectTypeDefinition inputObjectType:
                    foreach (var field in inputObjectType.Fields)
                    {
                        referencedTypeNames.Add(field.Type.NamedType().Name);
                    }

                    break;
            }
        }

        foreach (var directiveDefinition in schema.DirectiveDefinitions)
        {
            foreach (var argument in directiveDefinition.Arguments)
            {
                referencedTypeNames.Add(argument.Type.NamedType().Name);
            }
        }

        return referencedTypeNames;
    }

    /// <summary>
    /// Rewrites every application of Apollo's <c>@policy(policies: [[...]])</c> directive into
    /// Fusion's <c>@policy(names: [[...]])</c> shape, and replaces Apollo's directive definition
    /// with the canonical Fusion one. Apollo's <c>policies</c> and Fusion's <c>names</c> arguments
    /// share the same disjunctive-normal-form shape, so the argument value is carried over as is.
    /// Apollo's directive carries no denial behavior, so the rewritten applications omit
    /// <c>onDenied</c> and inherit the schema-wide default. Does nothing when the source schema
    /// does not link Apollo's policy spec.
    /// </summary>
    private static void TranslatePolicyDirective(
        MutableSchemaDefinition schema,
        ref MutableDirectiveDefinition? fusionPolicyDefinition)
    {
        var localName = ResolvePolicyLocalName(schema);

        if (localName is null)
        {
            return;
        }

        var applications = CollectDirectiveApplications(schema, localName, PoliciesArgumentName);

        if (applications.Count == 0)
        {
            return;
        }

        // Apollo's own @policy directive definition lives under this same name when unaliased,
        // and DirectiveDefinitions rejects a second definition under a name still in use, so the
        // old one is removed before the canonical Fusion definition is installed.
        schema.DirectiveDefinitions.Remove(localName);
        var definition = GetOrCreateFusionPolicyDefinition(schema, ref fusionPolicyDefinition);

        foreach (var (directives, directive) in applications)
        {
            directives.Replace(
                directive,
                new Directive(
                    definition,
                    new ArgumentAssignment(
                        WellKnownArgumentNames.Names,
                        directive.Arguments[PoliciesArgumentName])));
        }
    }

    /// <summary>
    /// Rewrites every application of Apollo's <c>@authenticated</c> and <c>@requiresScopes</c>
    /// directives into Fusion's <c>@policy(names:, onDenied: ERROR)</c> shape:
    /// <c>@authenticated</c> becomes a single application requiring the built-in
    /// <c>fusion.authenticated</c> policy, and <c>@requiresScopes(scopes: [[a, b], [c]])</c>
    /// becomes two cumulative applications, one requiring <c>fusion.authenticated</c> (a scope
    /// check implies authentication) and one requiring the built-in <c>fusion.scope:</c> policy
    /// for each scope, in the same disjunctive-normal-form shape Apollo declared. Both directives
    /// deny with an error, matching Apollo's own unauthenticated/unauthorized behavior. Applying
    /// both directives to the same element produces a redundant but harmless second authentication
    /// application. Does nothing when the source schema imports neither directive.
    /// </summary>
    private static void TranslateAuthDirectives(
        MutableSchemaDefinition schema,
        ref MutableDirectiveDefinition? fusionPolicyDefinition)
    {
        var authenticatedLocalName = ResolveAuthenticatedLocalName(schema);
        var requiresScopesLocalName = ResolveRequiresScopesLocalName(schema);

        if (authenticatedLocalName is null && requiresScopesLocalName is null)
        {
            return;
        }

        var authenticatedApplications = authenticatedLocalName is null
            ? []
            : CollectDirectiveApplications(schema, authenticatedLocalName);
        var requiresScopesApplications = requiresScopesLocalName is null
            ? []
            : CollectDirectiveApplications(schema, requiresScopesLocalName, ScopesArgumentName);

        if (authenticatedApplications.Count > 0 || requiresScopesApplications.Count > 0)
        {
            var definition = GetOrCreateFusionPolicyDefinition(schema, ref fusionPolicyDefinition);
            var authenticatedNames = CreateAuthenticatedNamesValue();

            foreach (var (directives, directive) in authenticatedApplications)
            {
                directives.Replace(
                    directive,
                    CreateErrorPolicyDirective(definition, authenticatedNames));
            }

            foreach (var (directives, directive) in requiresScopesApplications)
            {
                directives.Replace(
                    directive,
                    CreateErrorPolicyDirective(definition, authenticatedNames));
                directives.Add(
                    CreateErrorPolicyDirective(
                        definition,
                        TranslateScopeNames(directive.Arguments[ScopesArgumentName])));
            }
        }

        if (authenticatedLocalName is not null)
        {
            schema.DirectiveDefinitions.Remove(authenticatedLocalName);
        }

        if (requiresScopesLocalName is not null)
        {
            schema.DirectiveDefinitions.Remove(requiresScopesLocalName);
        }
    }

    /// <summary>
    /// Gets the canonical Fusion <c>@policy</c> directive definition already installed on the
    /// schema by an earlier translation in this same <see cref="Apply"/> call, or creates and
    /// installs it. <paramref name="fusionPolicyDefinition"/> is the sole source of truth for
    /// reuse: querying the schema's directive definitions by name is unsafe here, since Apollo's
    /// own (not yet removed) <c>@policy</c> definition can share the same unaliased name.
    /// </summary>
    private static MutableDirectiveDefinition GetOrCreateFusionPolicyDefinition(
        MutableSchemaDefinition schema,
        ref MutableDirectiveDefinition? fusionPolicyDefinition)
    {
        if (fusionPolicyDefinition is not null)
        {
            return fusionPolicyDefinition;
        }

        if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(
                SpecScalarNames.String.Name, out var stringType))
        {
            stringType = BuiltIns.String.Create();
        }

        if (!schema.Types.TryGetType<MutableEnumTypeDefinition>(
                WellKnownTypeNames.PolicyDenialBehavior, out var policyDenialBehaviorType))
        {
            policyDenialBehaviorType = PolicyDenialBehaviorMutableEnumTypeDefinition.Create();
            schema.Types.Add(policyDenialBehaviorType);
        }

        var definition = new PolicyMutableDirectiveDefinition(stringType, policyDenialBehaviorType);
        schema.DirectiveDefinitions.Add(definition);
        fusionPolicyDefinition = definition;
        return definition;
    }

    private static Directive CreateErrorPolicyDirective(
        MutableDirectiveDefinition fusionPolicyDefinition,
        IValueNode namesValue)
    {
        return new Directive(
            fusionPolicyDefinition,
            new ArgumentAssignment(WellKnownArgumentNames.Names, namesValue),
            new ArgumentAssignment(WellKnownArgumentNames.OnDenied, new EnumValueNode("ERROR")));
    }

    private static IValueNode CreateAuthenticatedNamesValue()
        => new ListValueNode(new ListValueNode(new StringValueNode(BuiltInPolicyNames.Authenticated)));

    /// <summary>
    /// Rewrites Apollo's <c>scopes: [[a, b], [c]]</c> disjunctive-normal-form value into Fusion's
    /// <c>names:</c> shape, prefixing every scope so that it addresses the built-in Fusion policy
    /// that checks for that scope.
    /// </summary>
    private static IValueNode TranslateScopeNames(IValueNode scopesValue)
    {
        var groupsNode = (ListValueNode)scopesValue;
        var groups = new IValueNode[groupsNode.Items.Count];

        for (var i = 0; i < groupsNode.Items.Count; i++)
        {
            var groupNode = (ListValueNode)groupsNode.Items[i];
            var names = new IValueNode[groupNode.Items.Count];

            for (var j = 0; j < groupNode.Items.Count; j++)
            {
                var scopeName = (StringValueNode)groupNode.Items[j];
                names[j] = new StringValueNode(BuiltInPolicyNames.ScopePrefix + scopeName.Value);
            }

            groups[i] = new ListValueNode(names);
        }

        return new ListValueNode(groups);
    }

    /// <summary>
    /// Resolves the local (possibly renamed via <c>@link(import: [{name, as}])</c>) name that
    /// Apollo's <c>@policy</c> directive was imported under, or <c>null</c> when the schema does
    /// not link Apollo's policy spec, or links it without importing <c>@policy</c>.
    /// </summary>
    internal static string? ResolvePolicyLocalName(MutableSchemaDefinition schema)
        => ResolveImportedDirectiveLocalName(schema, PolicySpecUrlPrefix, FederationDirectiveNames.Policy);

    /// <summary>
    /// Resolves the local (possibly renamed) name that Apollo's main federation spec link
    /// imports <c>@authenticated</c> under, or <c>null</c> when the schema does not import it.
    /// </summary>
    internal static string? ResolveAuthenticatedLocalName(MutableSchemaDefinition schema)
        => ResolveImportedDirectiveLocalName(
            schema, FederationSchemaAnalyzer.FederationUrlPrefix, FederationDirectiveNames.Authenticated);

    /// <summary>
    /// Resolves the local (possibly renamed) name that Apollo's main federation spec link
    /// imports <c>@requiresScopes</c> under, or <c>null</c> when the schema does not import it.
    /// </summary>
    internal static string? ResolveRequiresScopesLocalName(MutableSchemaDefinition schema)
        => ResolveImportedDirectiveLocalName(
            schema, FederationSchemaAnalyzer.FederationUrlPrefix, FederationDirectiveNames.RequiresScopes);

    /// <summary>
    /// Resolves the local (possibly renamed via <c>@link(import: [{name, as}])</c>) name that a
    /// directive spec-defined as <paramref name="canonicalName"/> was imported under, from the
    /// <c>@link</c> directive whose <c>url</c> contains <paramref name="urlPrefix"/>, or
    /// <c>null</c> when the schema does not link that spec, or links it without importing the
    /// directive.
    /// </summary>
    private static string? ResolveImportedDirectiveLocalName(
        MutableSchemaDefinition schema,
        string urlPrefix,
        string canonicalName)
    {
        foreach (var directive in schema.Directives[FederationDirectiveNames.Link])
        {
            if (!directive.Arguments.TryGetValue("url", out var urlValue)
                || urlValue is not StringValueNode urlString
                || !urlString.Value.Contains(urlPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            if (!directive.Arguments.TryGetValue("import", out var importValue)
                || importValue is not ListValueNode importList)
            {
                // Linked without an explicit import list: the directive is available under
                // its spec-defined name.
                return canonicalName;
            }

            foreach (var item in importList.Items)
            {
                switch (item)
                {
                    case StringValueNode importName
                        when TrimLeadingAt(importName.Value) == canonicalName:
                        return canonicalName;

                    case ObjectValueNode importObject:
                        var name = importObject.Fields.FirstOrDefault(f => f.Name.Value == "name")?.Value;

                        if (name is not StringValueNode nameNode
                            || TrimLeadingAt(nameNode.Value) != canonicalName)
                        {
                            continue;
                        }

                        var alias = importObject.Fields.FirstOrDefault(f => f.Name.Value == "as")?.Value;

                        return alias is StringValueNode aliasNode
                            ? TrimLeadingAt(aliasNode.Value)
                            : canonicalName;
                }
            }

            // The spec is linked, but the directive is not in its import list.
            return null;
        }

        return null;
    }

    private static string TrimLeadingAt(string value)
        => value.StartsWith('@') ? value[1..] : value;

    /// <summary>
    /// Collects every application of <paramref name="localName"/> on an object or interface type,
    /// or on a field of one, paired with the directive collection it lives in, so a caller can
    /// decide whether to install the canonical directive definition before mutating anything.
    /// Fusion's canonical <c>@policy</c> definition only allows the OBJECT and FIELD_DEFINITION
    /// locations, so applications on any other kind of type (for example a scalar or an enum) are
    /// left untouched here and reported by <see cref="FederationSchemaAnalyzer"/> instead.
    /// When <paramref name="requiredArgumentName"/> is given, an application missing that
    /// argument is skipped: it is malformed regardless of translation and is left for schema
    /// validation to report.
    /// </summary>
    private static List<(DirectiveCollection Directives, Directive Directive)> CollectDirectiveApplications(
        MutableSchemaDefinition schema,
        string localName,
        string? requiredArgumentName = null)
    {
        var applications = new List<(DirectiveCollection, Directive)>();

        foreach (var type in schema.Types)
        {
            if (type is not MutableComplexTypeDefinition complexType)
            {
                continue;
            }

            CollectDirectives(complexType.Directives, localName, requiredArgumentName, applications);

            foreach (var field in complexType.Fields)
            {
                CollectDirectives(field.Directives, localName, requiredArgumentName, applications);
            }
        }

        return applications;
    }

    private static void CollectDirectives(
        DirectiveCollection directives,
        string localName,
        string? requiredArgumentName,
        List<(DirectiveCollection, Directive)> applications)
    {
        foreach (var directive in directives[localName])
        {
            if (requiredArgumentName is null || directive.Arguments.ContainsName(requiredArgumentName))
            {
                applications.Add((directives, directive));
            }
        }
    }
}
