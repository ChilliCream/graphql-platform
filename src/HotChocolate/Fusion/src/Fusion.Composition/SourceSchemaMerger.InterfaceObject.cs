using HotChocolate.Features;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Features;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion;

internal sealed partial class SourceSchemaMerger
{
    /// <summary>
    /// Projects the fields contributed by <c>@interfaceObject</c> stand-ins onto the interfaces they
    /// stand in for and onto every implementing object type. The interface contract fields are
    /// already merged in by <see cref="MergeInterfaceTypes"/>; this step completes the implements
    /// relation with derived edges and adds each non-key contributed field as a default
    /// implementation on the implementing object types, routed to the source schema that owns it.
    /// </summary>
    private void ProjectInterfaceObjectFields(MutableSchemaDefinition mergedSchema)
    {
        var standInsByName = CollectStandIns();

        if (standInsByName.Count == 0)
        {
            return;
        }

        // The implements relation was completed by ApplyImplementsClosure before this step, so a
        // type reaches every interface it implements even through edges no single source schema
        // declared. Projection reads the completed relation.

        // The non-key, non-internal field names each interface's stand-ins contribute as defaults.
        var contributedByInterface = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var (interfaceName, standIns) in standInsByName)
        {
            var contributed = new HashSet<string>(StringComparer.Ordinal);

            foreach (var standIn in standIns)
            {
                foreach (var field in standIn.Type.Fields)
                {
                    if (field.IsInternal || standIn.KeyFieldNames.Contains(field.Name))
                    {
                        continue;
                    }

                    contributed.Add(field.Name);
                }
            }

            contributedByInterface[interfaceName] = contributed;
        }

        foreach (var objectType in mergedSchema.Types.OfType<MutableObjectTypeDefinition>())
        {
            // The contributed field names reachable through every interface this object implements.
            var candidateFieldNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (var interfaceType in objectType.Implements)
            {
                if (contributedByInterface.TryGetValue(interfaceType.Name, out var contributed))
                {
                    candidateFieldNames.UnionWith(contributed);
                }
            }

            foreach (var fieldName in candidateFieldNames)
            {
                // A direct declaration on the object type wins; a field already projected from a
                // more specific interface is likewise kept.
                if (objectType.Fields.ContainsName(fieldName))
                {
                    continue;
                }

                var owner = ResolveEffectiveOwner(objectType, fieldName, standInsByName);

                if (owner is null
                    || !owner.Value.Contributions[0].Interface.Fields.TryGetField(fieldName, out var contractField))
                {
                    continue;
                }

                var projected = new MutableOutputFieldDefinition(fieldName)
                {
                    Description = contractField.Description,
                    Type = contractField.Type
                };

                foreach (var contribution in owner.Value.Contributions)
                {
                    var schemaName = _schemaConstantNames[contribution.Schema.Name];

                    projected.Directives.Add(
                        new Directive(
                            _fusionDirectiveDefinitions[DirectiveNames.FusionField],
                            new ArgumentAssignment(
                                ArgumentNames.Schema,
                                new EnumValueNode(schemaName))));
                }

                foreach (var contribution in owner.Value.Contributions)
                {
                    var schemaName = _schemaConstantNames[contribution.Schema.Name];

                    if (!contribution.Interface.Fields.TryGetField(fieldName, out var contributingField))
                    {
                        continue;
                    }

                    foreach (var directive in contributingField.Directives[DirectiveNames.FusionRequires])
                    {
                        if (directive.Arguments[ArgumentNames.Schema] is EnumValueNode schema
                            && schema.Value == schemaName)
                        {
                            projected.Directives.Add(
                                new Directive(
                                    directive.Definition,
                                    directive.Arguments.AsEnumerable()));
                        }
                    }
                }

                projected.DeclaringMember = objectType;
                objectType.Fields.Add(projected);
            }
        }
    }

    /// <summary>
    /// Resolves the source schema(s) that own the default implementation of <paramref name="fieldName"/>
    /// on <paramref name="objectType"/>. The owner is the unique most specific stand-in that
    /// contributes the field; several incomparable minimal contributors are allowed only when every
    /// declaration is shareable (validated post-merge by <c>INVALID_PROJECTED_FIELD_SHARING</c>).
    /// </summary>
    private EffectiveOwner? ResolveEffectiveOwner(
        MutableObjectTypeDefinition objectType,
        string fieldName,
        Dictionary<string, List<StandInInfo>> standInsByName)
    {
        var candidates = new List<(StandInInfo StandIn, MutableInterfaceTypeDefinition Interface)>();

        foreach (var interfaceType in objectType.Implements)
        {
            if (!standInsByName.TryGetValue(interfaceType.Name, out var standIns))
            {
                continue;
            }

            foreach (var standIn in standIns)
            {
                if (standIn.KeyFieldNames.Contains(fieldName))
                {
                    continue;
                }

                if (standIn.Type.Fields.TryGetField(fieldName, out var field) && !field.IsInternal)
                {
                    candidates.Add((standIn, interfaceType));
                }
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        // Keep only the most specific contributors: a candidate whose interface is implemented by
        // another candidate's (different) interface is less specific and is dropped.
        var minimal = candidates
            .Where(c => !candidates.Any(
                other => !ReferenceEquals(other.Interface, c.Interface)
                    && other.Interface.Name != c.Interface.Name
                    && other.Interface.Implements.ContainsName(c.Interface.Name)))
            .ToList();

        var contributions = minimal
            .Select(c => new EffectiveOwnerContribution(c.Interface, c.StandIn.Schema))
            .DistinctBy(c => c.Schema.Name, StringComparer.Ordinal)
            .ToArray();

        return new EffectiveOwner(contributions);
    }

    /// <summary>
    /// Completes the implements relation of <paramref name="mergedSchema"/> so that implementation is
    /// transitive: whenever a type implements an interface, and that interface implements a parent
    /// interface, the type implements the parent as well. Source schemas hold only partial views of
    /// the hierarchy; this reconciles them.
    /// </summary>
    private static void ApplyImplementsClosure(MutableSchemaDefinition mergedSchema)
    {
        var edges = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var complexType in mergedSchema.Types.OfType<MutableComplexTypeDefinition>())
        {
            foreach (var interfaceType in complexType.Implements)
            {
                AddEdge(edges, complexType.Name, interfaceType.Name);
            }
        }

        // Transitively close the relation.
        var worklist = new Queue<(string Type, string Interface)>();

        foreach (var (typeName, interfaceNames) in edges)
        {
            foreach (var interfaceName in interfaceNames)
            {
                worklist.Enqueue((typeName, interfaceName));
            }
        }

        while (worklist.Count > 0)
        {
            var (typeName, interfaceName) = worklist.Dequeue();

            if (!edges.TryGetValue(interfaceName, out var parents))
            {
                continue;
            }

            foreach (var parent in parents)
            {
                if (AddEdge(edges, typeName, parent))
                {
                    worklist.Enqueue((typeName, parent));
                }
            }
        }

        // Apply any derived edges to the merged types.
        foreach (var complexType in mergedSchema.Types.OfType<MutableComplexTypeDefinition>())
        {
            if (!edges.TryGetValue(complexType.Name, out var interfaceNames))
            {
                continue;
            }

            foreach (var interfaceName in interfaceNames)
            {
                if (interfaceName == complexType.Name
                    || complexType.Implements.ContainsName(interfaceName))
                {
                    continue;
                }

                if (mergedSchema.Types.TryGetType(interfaceName, out MutableInterfaceTypeDefinition? interfaceType))
                {
                    complexType.Implements.Add(interfaceType);
                }
            }
        }
    }

    /// <summary>
    /// Applies <c>@override</c> declared on <c>@interfaceObject</c> stand-in fields. Before default
    /// implementations are resolved, every declaration of the overridden field in the named source
    /// schema is dropped across the target interface's implementation closure: on every implementing
    /// object type, on every more specific interface's stand-in, and on the named schema's own
    /// stand-in. Dropping the named schema's own stand-in declaration lets the default contributor
    /// role migrate from one schema to another.
    /// </summary>
    private void ApplyStandInOverrides()
    {
        var closure = ComputeSourceImplementsClosure();

        foreach (var schema in _schemas)
        {
            foreach (var type in schema.Types)
            {
                if (!IsInterfaceObjectStandIn(type))
                {
                    continue;
                }

                var standIn = (MutableObjectTypeDefinition)type;

                foreach (var field in standIn.Fields)
                {
                    if (GetOverrideFrom(field) is not { } fromName)
                    {
                        continue;
                    }

                    var fromSchema = _schemas.FirstOrDefault(s => s.Name == fromName);

                    // A dead override or an override naming the declaring schema itself drops
                    // nothing here; the existing @override rules report the invalid cases.
                    if (fromSchema is null || ReferenceEquals(fromSchema, schema))
                    {
                        continue;
                    }

                    foreach (var closureTypeName in GetOverrideClosureTypeNames(closure, standIn.Name))
                    {
                        if (fromSchema.Types.TryGetType(
                                closureTypeName,
                                out MutableObjectTypeDefinition? localType)
                            && localType.Fields.TryGetField(field.Name, out var localField))
                        {
                            localField.Features.GetOrSet<SourceOutputFieldMetadata>().IsOverridden = true;
                        }
                    }
                }
            }
        }
    }

    private Dictionary<string, HashSet<string>> ComputeSourceImplementsClosure()
    {
        var edges = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var schema in _schemas)
        {
            foreach (var type in schema.Types)
            {
                if (type is MutableComplexTypeDefinition complexType)
                {
                    foreach (var interfaceType in complexType.Implements)
                    {
                        AddEdge(edges, complexType.Name, interfaceType.Name);
                    }
                }
            }
        }

        var worklist = new Queue<(string Type, string Interface)>();

        foreach (var (typeName, interfaceNames) in edges)
        {
            foreach (var interfaceName in interfaceNames)
            {
                worklist.Enqueue((typeName, interfaceName));
            }
        }

        while (worklist.Count > 0)
        {
            var (typeName, interfaceName) = worklist.Dequeue();

            if (!edges.TryGetValue(interfaceName, out var parents))
            {
                continue;
            }

            foreach (var parent in parents)
            {
                if (AddEdge(edges, typeName, parent))
                {
                    worklist.Enqueue((typeName, parent));
                }
            }
        }

        return edges;
    }

    private static IEnumerable<string> GetOverrideClosureTypeNames(
        Dictionary<string, HashSet<string>> closure,
        string interfaceName)
    {
        // The interface name itself covers the named schema's own stand-in for the interface.
        yield return interfaceName;

        foreach (var (typeName, interfaceNames) in closure)
        {
            if (interfaceNames.Contains(interfaceName))
            {
                yield return typeName;
            }
        }
    }

    private static string? GetOverrideFrom(MutableOutputFieldDefinition field)
    {
        foreach (var directive in field.Directives.AsEnumerable())
        {
            if (directive.Name == DirectiveNames.Override
                && directive.Arguments.TryGetValue(ArgumentNames.From, out var value)
                && value is StringValueNode from)
            {
                return from.Value;
            }
        }

        return null;
    }

    private Dictionary<string, List<StandInInfo>> CollectStandIns()
    {
        var standInsByName = new Dictionary<string, List<StandInInfo>>(StringComparer.Ordinal);

        foreach (var schema in _schemas)
        {
            foreach (var type in schema.Types)
            {
                if (!IsInterfaceObjectStandIn(type))
                {
                    continue;
                }

                var standIn = (MutableObjectTypeDefinition)type;

                if (!standInsByName.TryGetValue(standIn.Name, out var list))
                {
                    list = [];
                    standInsByName[standIn.Name] = list;
                }

                list.Add(
                    new StandInInfo(
                        standIn,
                        schema,
                        InterfaceObjectMetadata.GetKeyFieldNames(standIn)));
            }
        }

        return standInsByName;
    }

    private static bool AddEdge(Dictionary<string, HashSet<string>> edges, string type, string @interface)
    {
        if (!edges.TryGetValue(type, out var interfaces))
        {
            interfaces = [with(StringComparer.Ordinal)];
            edges[type] = interfaces;
        }

        return interfaces.Add(@interface);
    }

    private readonly record struct StandInInfo(
        MutableObjectTypeDefinition Type,
        MutableSchemaDefinition Schema,
        HashSet<string> KeyFieldNames);

    private readonly record struct EffectiveOwner(EffectiveOwnerContribution[] Contributions);

    private readonly record struct EffectiveOwnerContribution(
        MutableInterfaceTypeDefinition Interface,
        MutableSchemaDefinition Schema);
}
