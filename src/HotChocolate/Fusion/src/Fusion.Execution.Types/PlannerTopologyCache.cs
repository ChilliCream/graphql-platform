using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Types.Metadata;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

/// <summary>
/// Caches topology data used by the operation planner.
/// </summary>
internal sealed class PlannerTopologyCache
{
    private readonly FrozenDictionary<FieldKey, FieldResolutionInfo> _fieldResolutions;
    private readonly FrozenDictionary<LookupKey, ImmutableArray<Lookup>> _orderedLookups;
    private readonly FrozenDictionary<TransitionKey, Lookup> _directTransitions;
    private readonly FrozenDictionary<string, TypeScatterInfo> _typeScatter;
    private readonly FrozenSet<ContextKey> _contexts;

    private PlannerTopologyCache(
        FrozenDictionary<FieldKey, FieldResolutionInfo> fieldResolutions,
        FrozenDictionary<LookupKey, ImmutableArray<Lookup>> orderedLookups,
        FrozenDictionary<TransitionKey, Lookup> directTransitions,
        FrozenDictionary<string, TypeScatterInfo> typeScatter,
        FrozenSet<ContextKey> contexts)
    {
        _fieldResolutions = fieldResolutions;
        _orderedLookups = orderedLookups;
        _directTransitions = directTransitions;
        _typeScatter = typeScatter;
        _contexts = contexts;
    }

    public static PlannerTopologyCache Build(FusionSchemaDefinition schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var complexTypes = schema.Types.AsEnumerable()
            .OfType<FusionComplexTypeDefinition>()
            .Where(t => !((ITypeDefinition)t).IsIntrospectionType)
            .ToArray();
        var schemaNames = CollectSchemaNames(complexTypes);
        var fieldResolutions = BuildFieldResolutions(complexTypes);
        var orderedLookups = BuildOrderedLookups(schema, complexTypes);
        var directTransitions = BuildTransitions(schema, complexTypes, schemaNames);
        var typeScatter = BuildTypeScatter(complexTypes, fieldResolutions);
        var contexts = BuildContexts(schema, complexTypes);

        return new PlannerTopologyCache(
            fieldResolutions.ToFrozenDictionary(),
            orderedLookups.ToFrozenDictionary(),
            directTransitions.ToFrozenDictionary(),
            typeScatter.ToFrozenDictionary(StringComparer.Ordinal),
            contexts.ToFrozenSet());
    }

    public bool TryGetFieldResolution(
        string typeName,
        string fieldName,
        out FieldResolutionInfo fieldResolution)
        => _fieldResolutions.TryGetValue(new FieldKey(typeName, fieldName), out fieldResolution);

    public bool TryGetOrderedLookups(
        string typeName,
        string? schemaName,
        out ImmutableArray<Lookup> lookups)
        => _orderedLookups.TryGetValue(new LookupKey(typeName, schemaName), out lookups);

    public bool TryGetDirectTransition(
        string typeName,
        string fromSchema,
        string toSchema,
        [NotNullWhen(true)]
        out Lookup? lookup)
        => _directTransitions.TryGetValue(new TransitionKey(typeName, fromSchema, toSchema), out lookup);

    public bool TryGetTypeScatter(
        string typeName,
        out TypeScatterInfo scatter)
        => _typeScatter.TryGetValue(typeName, out scatter);

    public bool SetsContext(string typeName, string schemaName, string contextName)
        => _contexts.Contains(new ContextKey(typeName, schemaName, contextName));

    private static HashSet<string> CollectSchemaNames(
        IEnumerable<FusionComplexTypeDefinition> complexTypes)
    {
        var schemaNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var complexType in complexTypes)
        {
            foreach (var source in complexType.Sources)
            {
                schemaNames.Add(source.SchemaName);
            }
        }

        return schemaNames;
    }

    private static Dictionary<FieldKey, FieldResolutionInfo> BuildFieldResolutions(
        IEnumerable<FusionComplexTypeDefinition> complexTypes)
    {
        var fieldResolutions = new Dictionary<FieldKey, FieldResolutionInfo>();

        foreach (var complexType in complexTypes)
        {
            foreach (var field in complexType.Fields.AsEnumerable(allowInaccessibleFields: true))
            {
                var schemas = field.Sources.Schemas
                    .OrderBy(static s => s, StringComparer.Ordinal)
                    .ToImmutableArray();

                var requirementSchemas =
                    field.Sources.Members
                        .Where(static s => s.Requirements is not null)
                        .Select(static s => s.SchemaName)
                        .OrderBy(static s => s, StringComparer.Ordinal)
                        .ToImmutableArray();

                fieldResolutions[new FieldKey(complexType.Name, field.Name)] =
                    new FieldResolutionInfo(schemas, requirementSchemas);
            }
        }

        return fieldResolutions;
    }

    private static Dictionary<LookupKey, ImmutableArray<Lookup>> BuildOrderedLookups(
        FusionSchemaDefinition schema,
        IEnumerable<FusionComplexTypeDefinition> complexTypes)
    {
        var orderedLookups = new Dictionary<LookupKey, ImmutableArray<Lookup>>();

        foreach (var complexType in complexTypes)
        {
            orderedLookups[new LookupKey(complexType.Name, null)] =
                OrderLookups(schema.GetPossibleLookups(complexType));

            foreach (var source in complexType.Sources)
            {
                var schemaName = source.SchemaName;
                orderedLookups[new LookupKey(complexType.Name, schemaName)] =
                    OrderLookups(schema.GetPossibleLookups(complexType, schemaName));
            }
        }

        return orderedLookups;
    }

    private static Dictionary<TransitionKey, Lookup> BuildTransitions(
        FusionSchemaDefinition schema,
        IEnumerable<FusionComplexTypeDefinition> complexTypes,
        IEnumerable<string> schemaNames)
    {
        var directTransitions = new Dictionary<TransitionKey, Lookup>();
        var visitor = new KeyTransitionVisitor();

        foreach (var complexType in complexTypes)
        {
            foreach (var toSchema in GetTransitionTargetSchemas(complexType))
            {
                // A direct transition can only target a schema that has lookups for this
                // type; if there are none, no from-schema can transition here, so skip the sweep.
                if (schema.GetPossibleLookups(complexType, toSchema).Length == 0)
                {
                    continue;
                }

                foreach (var fromSchema in schemaNames)
                {
                    var bestLookup = FindBestDirectLookup(schema, visitor, complexType, fromSchema, toSchema);

                    if (bestLookup is not null)
                    {
                        directTransitions[new TransitionKey(complexType.Name, fromSchema, toSchema)] = bestLookup;
                    }
                }
            }
        }

        return directTransitions;
    }

    // The schemas a transition may target for a type: the schemas that declare the type, plus the
    // @interfaceObject stand-in schemas of the interfaces it implements. A stand-in holds no source
    // for the concrete type yet resolves its projected fields through a single covering interface
    // lookup, so a value must be able to transition there under a concrete type condition. Bounded
    // to stand-in schemas, this leaves the transition sweep unchanged for schemas without
    // interfaceObject.
    private static IEnumerable<string> GetTransitionTargetSchemas(FusionComplexTypeDefinition complexType)
    {
        var targets = new HashSet<string>(StringComparer.Ordinal);

        foreach (var source in complexType.Sources)
        {
            targets.Add(source.SchemaName);
        }

        foreach (var interfaceType in complexType.Implements)
        {
            foreach (var interfaceSource in interfaceType.Sources)
            {
                if (interfaceSource.IsInterfaceObject
                    && !complexType.Sources.TryGetMember(interfaceSource.SchemaName, out _))
                {
                    targets.Add(interfaceSource.SchemaName);
                }
            }
        }

        return targets;
    }

    private static Lookup? FindBestDirectLookup(
        FusionSchemaDefinition schema,
        KeyTransitionVisitor visitor,
        FusionComplexTypeDefinition type,
        string fromSchema,
        string toSchema)
    {
        var context = new KeyTransitionVisitor.Context
        {
            CompositeSchema = schema,
            SourceSchema = fromSchema,
            Types = [type]
        };

        Lookup? bestLookup = null;
        var fields = 0;
        var fragments = 0;

        foreach (var possibleLookup in schema.GetPossibleLookups(type, toSchema))
        {
            context.Reset();
            visitor.Visit(possibleLookup.Requirements, context);

            if (context.NeedsTransition)
            {
                continue;
            }

            if (context is { Fields: 1, Fragments: 0 })
            {
                return possibleLookup;
            }

            if (bestLookup is null)
            {
                bestLookup = possibleLookup;
                fields = context.Fields;
                fragments = context.Fragments;
                continue;
            }

            if (context.Fields < fields)
            {
                bestLookup = possibleLookup;
                fields = context.Fields;
                fragments = context.Fragments;
            }

            if (context.Fields == fields && context.Fragments < fragments)
            {
                bestLookup = possibleLookup;
                fields = context.Fields;
                fragments = context.Fragments;
            }
        }

        return bestLookup;
    }

    private static Dictionary<string, TypeScatterInfo> BuildTypeScatter(
        IEnumerable<FusionComplexTypeDefinition> complexTypes,
        IReadOnlyDictionary<FieldKey, FieldResolutionInfo> fieldResolutions)
    {
        var scatter = new Dictionary<string, TypeScatterInfo>(StringComparer.Ordinal);

        foreach (var complexType in complexTypes)
        {
            var totalFields = 0;
            var coverage = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var field in complexType.Fields.AsEnumerable(allowInaccessibleFields: true))
            {
                totalFields++;

                if (!fieldResolutions.TryGetValue(new FieldKey(complexType.Name, field.Name), out var resolution))
                {
                    continue;
                }

                foreach (var schemaName in resolution.Schemas)
                {
                    coverage[schemaName] = coverage.GetValueOrDefault(schemaName, 0) + 1;
                }
            }

            var schemaCount = coverage.Count;
            var maxCoverage = coverage.Count == 0 ? 0 : coverage.Values.Max();
            var scatterRatio =
                totalFields == 0 || schemaCount <= 1
                    ? 0.0
                    : 1.0 - (double)maxCoverage / totalFields;

            scatter[complexType.Name] = new TypeScatterInfo(totalFields, schemaCount, maxCoverage, scatterRatio);
        }

        return scatter;
    }

    // An object type sets a context in a source schema when the type itself, an interface it
    // implements in that schema, or a union it is a member of in that schema declares the context.
    private static HashSet<ContextKey> BuildContexts(
        FusionSchemaDefinition schema,
        IEnumerable<FusionComplexTypeDefinition> complexTypes)
    {
        var contexts = new HashSet<ContextKey>();
        var unionTypes = schema.Types.AsEnumerable().OfType<FusionUnionTypeDefinition>().ToArray();

        foreach (var objectType in complexTypes.OfType<FusionObjectTypeDefinition>())
        {
            foreach (var source in objectType.Sources)
            {
                foreach (var contextName in source.Contexts)
                {
                    contexts.Add(new ContextKey(objectType.Name, source.SchemaName, contextName));
                }

                foreach (var interfaceType in objectType.Implements)
                {
                    if (source.Implements.Contains(interfaceType.Name)
                        && interfaceType.Sources.TryGetMember(source.SchemaName, out var interfaceSource))
                    {
                        foreach (var contextName in interfaceSource.Contexts)
                        {
                            contexts.Add(new ContextKey(objectType.Name, source.SchemaName, contextName));
                        }
                    }
                }

                foreach (var unionType in unionTypes)
                {
                    if (source.MemberOf.Contains(unionType.Name)
                        && unionType.Sources.TryGetMember(source.SchemaName, out var unionSource))
                    {
                        foreach (var contextName in unionSource.Contexts)
                        {
                            contexts.Add(new ContextKey(objectType.Name, source.SchemaName, contextName));
                        }
                    }
                }
            }
        }

        return contexts;
    }

    private static ImmutableArray<Lookup> OrderLookups(ImmutableArray<Lookup> lookups)
        => [.. lookups.OrderBy(CreateLookupOrderingKey, StringComparer.Ordinal)];

    private static string CreateLookupOrderingKey(Lookup lookup)
    {
        var path = lookup.Path.Length == 0
            ? string.Empty
            : string.Join('.', lookup.Path);

        return string.Concat(
            lookup.SchemaName,
            ":",
            lookup.FieldName,
            ":",
            path,
            ":",
            lookup.Arguments.Length.ToString(),
            ":",
            lookup.Fields.Length.ToString());
    }

    private readonly record struct FieldKey(string TypeName, string FieldName);

    private readonly record struct LookupKey(string TypeName, string? SchemaName);

    private readonly record struct ContextKey(string TypeName, string SchemaName, string ContextName);

    private readonly record struct TransitionKey(string TypeName, string FromSchema, string ToSchema);
}
