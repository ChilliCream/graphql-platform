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

    private static readonly HashSet<string> s_federationDirectiveNames =
    [
        with(StringComparer.Ordinal),
        FederationDirectiveNames.Key,
        FederationDirectiveNames.Requires,
        FederationDirectiveNames.Provides,
        FederationDirectiveNames.External,
        FederationDirectiveNames.Link,
        FederationDirectiveNames.Shareable,
        FederationDirectiveNames.Inaccessible,
        FederationDirectiveNames.Override,
        FederationDirectiveNames.Tag,
        FederationDirectiveNames.ComposeDirective,
        FederationDirectiveNames.Authenticated,
        FederationDirectiveNames.RequiresScopes
    ];

    private static readonly HashSet<string> s_federationScalarNames =
    [
        with(StringComparer.Ordinal),
        FederationTypeNames.Any,
        FederationTypeNames.FieldSet,
        FederationTypeNames.LegacyFieldSet,
        FederationTypeNames.Policy
    ];

    /// <summary>
    /// Applies the transformation to remove federation infrastructure from the schema.
    /// </summary>
    /// <param name="schema">
    /// The mutable schema definition to transform in place.
    /// </param>
    public static void Apply(MutableSchemaDefinition schema)
    {
        // Rewrite Apollo's @policy applications into Fusion's @policy(names:) shape before the
        // Apollo directive definitions are dropped below, so the authorization semantic survives
        // the import instead of being silently discarded.
        TranslatePolicyDirective(schema);

        // Remove federation directive definitions.
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
        var linkDirectives = schema.Directives[FederationDirectiveNames.Link].ToList();

        foreach (var directive in linkDirectives)
        {
            schema.Directives.Remove(directive);
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
    private static void TranslatePolicyDirective(MutableSchemaDefinition schema)
    {
        var localName = ResolvePolicyLocalName(schema);

        if (localName is null)
        {
            return;
        }

        var applications = CollectPolicyApplications(schema, localName);

        if (applications.Count == 0)
        {
            return;
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

        var fusionPolicyDefinition = new PolicyMutableDirectiveDefinition(stringType, policyDenialBehaviorType);

        foreach (var (directives, directive) in applications)
        {
            directives.Replace(
                directive,
                new Directive(
                    fusionPolicyDefinition,
                    new ArgumentAssignment(
                        WellKnownArgumentNames.Names,
                        directive.Arguments[PoliciesArgumentName])));
        }

        schema.DirectiveDefinitions.Remove(localName);
        schema.DirectiveDefinitions.Add(fusionPolicyDefinition);
    }

    /// <summary>
    /// Resolves the local (possibly renamed via <c>@link(import: [{name, as}])</c>) name that
    /// Apollo's <c>@policy</c> directive was imported under, or <c>null</c> when the schema does
    /// not link Apollo's policy spec, or links it without importing <c>@policy</c>.
    /// </summary>
    internal static string? ResolvePolicyLocalName(MutableSchemaDefinition schema)
    {
        foreach (var directive in schema.Directives[FederationDirectiveNames.Link])
        {
            if (!directive.Arguments.TryGetValue("url", out var urlValue)
                || urlValue is not StringValueNode urlString
                || !urlString.Value.Contains(PolicySpecUrlPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            if (!directive.Arguments.TryGetValue("import", out var importValue)
                || importValue is not ListValueNode importList)
            {
                // Linked without an explicit import list: the directive is available under
                // its spec-defined name.
                return FederationDirectiveNames.Policy;
            }

            foreach (var item in importList.Items)
            {
                switch (item)
                {
                    case StringValueNode importName
                        when TrimLeadingAt(importName.Value) == FederationDirectiveNames.Policy:
                        return FederationDirectiveNames.Policy;

                    case ObjectValueNode importObject:
                        var name = importObject.Fields.FirstOrDefault(f => f.Name.Value == "name")?.Value;

                        if (name is not StringValueNode nameNode
                            || TrimLeadingAt(nameNode.Value) != FederationDirectiveNames.Policy)
                        {
                            continue;
                        }

                        var alias = importObject.Fields.FirstOrDefault(f => f.Name.Value == "as")?.Value;

                        return alias is StringValueNode aliasNode
                            ? TrimLeadingAt(aliasNode.Value)
                            : FederationDirectiveNames.Policy;
                }
            }

            // The policy spec is linked, but @policy is not in its import list.
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
    /// Applications missing the <c>policies</c> argument are skipped: they are malformed
    /// regardless of translation and are left for schema validation to report.
    /// </summary>
    private static List<(DirectiveCollection Directives, Directive Directive)> CollectPolicyApplications(
        MutableSchemaDefinition schema,
        string localName)
    {
        var applications = new List<(DirectiveCollection, Directive)>();

        foreach (var type in schema.Types)
        {
            if (type is not MutableComplexTypeDefinition complexType)
            {
                continue;
            }

            CollectDirectives(complexType.Directives, localName, applications);

            foreach (var field in complexType.Fields)
            {
                CollectDirectives(field.Directives, localName, applications);
            }
        }

        return applications;
    }

    private static void CollectDirectives(
        DirectiveCollection directives,
        string localName,
        List<(DirectiveCollection, Directive)> applications)
    {
        foreach (var directive in directives[localName])
        {
            if (directive.Arguments.ContainsName(PoliciesArgumentName))
            {
                applications.Add((directives, directive));
            }
        }
    }
}
