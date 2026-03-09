using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Types.Metadata;

namespace HotChocolate.Fusion.Types;

internal sealed class PlannerTopologyCache
{
    private readonly FrozenDictionary<FieldKey, FieldResolutionInfo> _fieldResolutions;
    private readonly FrozenDictionary<LookupKey, ImmutableArray<Lookup>> _orderedLookups;
    private readonly FrozenDictionary<TransitionKey, Lookup> _directTransitions;
    private readonly FrozenSet<TransitionKey> _impossibleDirectTransitions;
    private readonly FrozenDictionary<string, TypeScatterInfo> _typeScatter;

    private PlannerTopologyCache(
        FrozenDictionary<FieldKey, FieldResolutionInfo> fieldResolutions,
        FrozenDictionary<LookupKey, ImmutableArray<Lookup>> orderedLookups,
        FrozenDictionary<TransitionKey, Lookup> directTransitions,
        FrozenSet<TransitionKey> impossibleDirectTransitions,
        FrozenDictionary<string, TypeScatterInfo> typeScatter)
    {
        _fieldResolutions = fieldResolutions;
        _orderedLookups = orderedLookups;
        _directTransitions = directTransitions;
        _impossibleDirectTransitions = impossibleDirectTransitions;
        _typeScatter = typeScatter;
    }

    public static PlannerTopologyCache Build(FusionSchemaDefinition schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var complexTypes = schema.Types.AsEnumerable().OfType<FusionComplexTypeDefinition>().ToArray();
        var schemaNames = CollectSchemaNames(complexTypes);
        var fieldResolutions = BuildFieldResolutions(complexTypes);
        var orderedLookups = BuildOrderedLookups(schema, complexTypes, schemaNames);
        var (directTransitions, impossibleDirectTransitions) = BuildTransitions(schema, complexTypes, schemaNames);
        var typeScatter = BuildTypeScatter(complexTypes, fieldResolutions);

        return new PlannerTopologyCache(
            fieldResolutions.ToFrozenDictionary(),
            orderedLookups.ToFrozenDictionary(),
            directTransitions.ToFrozenDictionary(),
            impossibleDirectTransitions.ToFrozenSet(),
            typeScatter.ToFrozenDictionary(StringComparer.Ordinal));
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

    public bool IsDirectTransitionImpossible(
        string typeName,
        string fromSchema,
        string toSchema)
        => _impossibleDirectTransitions.Contains(new TransitionKey(typeName, fromSchema, toSchema));

    public bool TryGetTypeScatter(
        string typeName,
        out TypeScatterInfo scatter)
        => _typeScatter.TryGetValue(typeName, out scatter);

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
        IEnumerable<FusionComplexTypeDefinition> complexTypes,
        IEnumerable<string> schemaNames)
    {
        var orderedLookups = new Dictionary<LookupKey, ImmutableArray<Lookup>>();

        foreach (var complexType in complexTypes)
        {
            orderedLookups[new LookupKey(complexType.Name, null)] =
                OrderLookups(schema.GetPossibleLookups(complexType));

            foreach (var schemaName in schemaNames)
            {
                orderedLookups[new LookupKey(complexType.Name, schemaName)] =
                    OrderLookups(schema.GetPossibleLookups(complexType, schemaName));
            }
        }

        return orderedLookups;
    }

    private static (
        Dictionary<TransitionKey, Lookup> DirectTransitions,
        HashSet<TransitionKey> ImpossibleDirectTransitions)
        BuildTransitions(
            FusionSchemaDefinition schema,
            IEnumerable<FusionComplexTypeDefinition> complexTypes,
            IEnumerable<string> schemaNames)
    {
        var directTransitions = new Dictionary<TransitionKey, Lookup>();
        var impossibleDirectTransitions = new HashSet<TransitionKey>();

        foreach (var complexType in complexTypes)
        {
            foreach (var fromSchema in schemaNames)
            {
                foreach (var toSchema in schemaNames)
                {
                    var key = new TransitionKey(complexType.Name, fromSchema, toSchema);

                    if (schema.TryGetBestDirectLookup(complexType, fromSchema, toSchema, out var lookup))
                    {
                        directTransitions[key] = lookup;
                    }
                    else
                    {
                        impossibleDirectTransitions.Add(key);
                    }
                }
            }
        }

        return (directTransitions, impossibleDirectTransitions);
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

    private readonly record struct TransitionKey(string TypeName, string FromSchema, string ToSchema);
}

internal readonly record struct FieldResolutionInfo(
    ImmutableArray<string> Schemas,
    ImmutableArray<string> SchemasWithRequirements)
{
    public bool ContainsSchema(string schemaName)
    {
        foreach (var candidate in Schemas)
        {
            if (candidate.Equals(schemaName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public bool HasRequirements(string schemaName)
    {
        foreach (var candidate in SchemasWithRequirements)
        {
            if (candidate.Equals(schemaName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

internal readonly record struct TypeScatterInfo(
    int TotalFields,
    int SchemaCount,
    int MaxCoverage,
    double ScatterRatio);
