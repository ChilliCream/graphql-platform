using System.Collections.Immutable;
using HotChocolate.Fusion.ApolloFederation;
using HotChocolate.Fusion.Language;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using BooleanValueNode = HotChocolate.Language.BooleanValueNode;
using ListValueNode = HotChocolate.Language.ListValueNode;
using StringValueNode = HotChocolate.Language.StringValueNode;

namespace HotChocolate.Fusion.Extensions;

internal static class MutableSchemaDefinitionExtensions
{
    public static void AddBuiltInFusionTypes(this MutableSchemaDefinition schema)
    {
        foreach (var builtInScalar in FusionBuiltIns.SourceSchemaScalars.Values)
        {
            schema.Types.Add(builtInScalar);
        }
    }

    public static void AddBuiltInFusionDirectives(this MutableSchemaDefinition schema)
    {
        foreach (var builtInDirective in FusionBuiltIns.SourceSchemaDirectives.Values)
        {
            schema.DirectiveDefinitions.Add(builtInDirective);
        }
    }

    public static bool IsRootOperationType(
        this MutableSchemaDefinition schema,
        MutableObjectTypeDefinition type)
    {
        return schema.QueryType == type || schema.MutationType == type || schema.SubscriptionType == type;
    }

    public static List<IDirective> GetPossibleFusionLookupDirectives(
        this MutableSchemaDefinition schema,
        MutableComplexTypeDefinition type,
        string? schemaName = null)
    {
        if (!string.IsNullOrEmpty(schemaName)
            && !type.ExistsInSchema(schemaName)
            && !ReachesInterfaceObjectStandIn(schema, type, schemaName))
        {
            return [];
        }

        var unionsContainingType = type.Kind == TypeKind.Object
            ? schema.Types
                .OfType<MutableUnionTypeDefinition>()
                .Where(u => u.Types.Contains(type))
                .ToImmutableArray()
            : [];

        return GetPossibleFusionLookupDirectivesCore(schema, type, schemaName, unionsContainingType);
    }

    internal static List<IDirective> GetPossibleFusionLookupDirectivesCore(
        MutableSchemaDefinition schema,
        MutableComplexTypeDefinition type,
        string? schemaName,
        IReadOnlyList<MutableUnionTypeDefinition> unionsContainingType)
    {
        // Get the lookups directly on the requested type.
        var lookups = GetFusionLookupDirectives(type, schemaName);

        // Get the lookups of interfaces this type implements.
        var implementsDirectives = type.Directives
            .AsEnumerable()
            .Where(d => d.Name == WellKnownDirectiveNames.FusionImplements)
            .ToImmutableArray();

        foreach (var implementsDirective in implementsDirectives)
        {
            var implementedInSchemaName = (string)implementsDirective.Arguments[WellKnownArgumentNames.Schema].Value!;

            if (!string.IsNullOrEmpty(schemaName) && implementedInSchemaName != schemaName)
            {
                continue;
            }

            var interfaceName = (string)implementsDirective.Arguments[WellKnownArgumentNames.Interface].Value!;

            if (!schema.Types.TryGetType<MutableInterfaceTypeDefinition>(interfaceName, out var interfaceType))
            {
                continue;
            }

            var interfaceLookupsInSchema =
                GetFusionLookupDirectives(interfaceType, implementedInSchemaName);

            lookups.AddRange(interfaceLookupsInSchema);
        }

        // Get the interface lookups of any @interfaceObject stand-in for an interface this type
        // implements. A stand-in resolves every possible type of its interface by the shared key,
        // so its interface-typed lookups are available for this type even in schemas that do not
        // define the type. The implements relation is read from the completed set on the type,
        // which includes edges derived by transitive closure.
        var standInInterfaceNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var implementedInterface in type.Implements)
        {
            if (!standInInterfaceNames.Add(implementedInterface.Name)
                || !schema.Types.TryGetType<MutableInterfaceTypeDefinition>(
                    implementedInterface.Name,
                    out var standInInterfaceType))
            {
                continue;
            }

            foreach (var standInSchemaName in GetInterfaceObjectSchemaNames(standInInterfaceType))
            {
                if (!string.IsNullOrEmpty(schemaName) && standInSchemaName != schemaName)
                {
                    continue;
                }

                lookups.AddRange(GetFusionLookupDirectives(standInInterfaceType, standInSchemaName));
            }
        }

        // Get the lookups of unions this type is a member of,
        // if it's an object type.
        if (type.Kind == TypeKind.Object)
        {
            foreach (var unionType in unionsContainingType)
            {
                if (!string.IsNullOrEmpty(schemaName) && !unionType.ExistsInSchema(schemaName))
                {
                    continue;
                }

                var unionMemberDirectives = unionType.Directives
                    .AsEnumerable()
                    .Where(d => d.Name == WellKnownDirectiveNames.FusionUnionMember
                        && (string)d.Arguments[WellKnownArgumentNames.Member].Value! == type.Name)
                    .ToImmutableArray();

                foreach (var unionMemberDirective in unionMemberDirectives)
                {
                    var memberInSchemaName =
                        (string)unionMemberDirective.Arguments[WellKnownArgumentNames.Schema].Value!;

                    if (!string.IsNullOrEmpty(schemaName) && memberInSchemaName != schemaName)
                    {
                        continue;
                    }

                    var unionLookups = GetFusionLookupDirectives(unionType, schemaName);

                    lookups.AddRange(unionLookups);
                }
            }
        }

        return lookups;
    }

    public static List<IDirective> GetPossibleFusionLookupDirectivesById(
        this MutableSchemaDefinition schema,
        MutableComplexTypeDefinition type,
        string? schemaName = null)
    {
        var lookups = GetPossibleFusionLookupDirectives(schema, type, schemaName);
        var lookupsById = new List<IDirective>();

        foreach (var lookup in lookups)
        {
            if (lookup.Arguments[WellKnownArgumentNames.Map] is ListValueNode { Items.Count: 1 } mapArg
                && mapArg.Items[0].Value?.Equals(WellKnownArgumentNames.Id) == true
                && lookup.Arguments[WellKnownArgumentNames.Internal] is not BooleanValueNode { Value: true })
            {
                lookupsById.Add(lookup);
            }
        }

        return lookupsById;
    }

    /// <summary>
    /// Removes type system definitions that are not reachable from operation roots or preserved
    /// types, then removes any <c>@external</c> fields the removals left unreferenced, repeating
    /// until the schema is stable.
    /// </summary>
    /// <param name="schema">
    /// The schema from which unreferenced definitions are removed.
    /// </param>
    /// <param name="preservedTypeNames">
    /// Type names that are always treated as reachable.
    /// </param>
    /// <param name="seedUnionsAsRoots">
    /// A source schema's union definition is a piecewise contribution to the merged union.
    /// Reachability of a union is a merged-schema property, so per-source pruning must seed
    /// union definitions as roots. The merged-schema prune passes false and remains the cleanup
    /// for genuinely dead unions.
    /// </param>
    public static void RemoveUnreferencedDefinitions(
        this MutableSchemaDefinition schema,
        IReadOnlySet<string> preservedTypeNames,
        bool seedUnionsAsRoots)
    {
        RemoveUnreachableDefinitions(schema, preservedTypeNames, seedUnionsAsRoots);

        // Removing an unreachable type also removes the @require/@provides selections it carried.
        // An @external field whose only references came from those selections is now dead, so
        // prune it and re-run reachability: dropping the field can orphan its return type, whose
        // own requirement selections can in turn orphan further externals. This converges quickly,
        // and schemas with no dead externals never re-run reachability.
        while (RemoveExternalFields.RemoveDeadExternalFields(schema))
        {
            RemoveUnreachableDefinitions(schema, preservedTypeNames, seedUnionsAsRoots);
        }
    }

    private static void RemoveUnreachableDefinitions(
        MutableSchemaDefinition schema,
        IReadOnlySet<string> preservedTypeNames,
        bool seedUnionsAsRoots)
    {
        var touchedDefinitions = new HashSet<ITypeSystemMember>();
        var backlog = new Stack<ITypeSystemMember>();

        foreach (var directive in schema.Directives)
        {
            backlog.Push(directive.Definition);
        }

        if (schema.QueryType is not null)
        {
            backlog.Push(schema.QueryType);
        }

        if (schema.MutationType is not null)
        {
            backlog.Push(schema.MutationType);
        }

        if (schema.SubscriptionType is not null)
        {
            backlog.Push(schema.SubscriptionType);
        }

        foreach (var typeName in preservedTypeNames)
        {
            if (schema.Types.TryGetType(typeName, out var inputType))
            {
                backlog.Push(inputType);
            }
        }

        // A union type definition in a source schema is a piecewise contribution to the
        // merged union. Because a union's reachability is a merged-schema property, per-source
        // pruning seeds union definitions as roots so member contributions that only become
        // reachable through another source schema's fields are not eaten before the merge.
        // The merged-schema prune (which passes seedUnionsAsRoots: false) still removes unions
        // that are unreachable in every source.
        if (seedUnionsAsRoots)
        {
            foreach (var type in schema.Types)
            {
                if (type is IUnionTypeDefinition)
                {
                    backlog.Push(type);
                }
            }
        }

        while (backlog.TryPop(out var type))
        {
            if (!touchedDefinitions.Add(type))
            {
                continue;
            }

            switch (type)
            {
                case IComplexTypeDefinition complexType:
                    InspectComplexType(schema, complexType, backlog);
                    break;

                case IDirectiveDefinition directiveDefinition:
                    InspectDirectiveDefinition(directiveDefinition, backlog);
                    break;

                case IEnumTypeDefinition enumType:
                    InspectEnumType(enumType, backlog);
                    break;

                case IInputObjectTypeDefinition inputObjectType:
                    InspectInputObjectType(inputObjectType, backlog);
                    break;

                case IUnionTypeDefinition unionType:
                    InspectUnionType(unionType, backlog);
                    break;

                case IDirectivesProvider directivesProvider:
                    foreach (var directive in directivesProvider.Directives)
                    {
                        backlog.Push(directive.Definition);
                    }

                    break;
            }
        }

        var definitionsToRemove = new HashSet<ITypeSystemMember>();
        foreach (var type in schema.Types)
        {
            if (touchedDefinitions.Contains(type))
            {
                continue;
            }

            definitionsToRemove.Add(type);
        }

        foreach (var directiveDefinition in schema.DirectiveDefinitions)
        {
            if (touchedDefinitions.Contains(directiveDefinition))
            {
                continue;
            }

            definitionsToRemove.Add(directiveDefinition);
        }

        if (definitionsToRemove.Count > 0)
        {
            foreach (var definition in definitionsToRemove)
            {
                switch (definition)
                {
                    case ITypeDefinition typeDefinition:
                        schema.Types.Remove(typeDefinition);
                        break;

                    case MutableDirectiveDefinition directiveDefinition:
                        schema.DirectiveDefinitions.Remove(directiveDefinition);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Gets the source schema names in which <paramref name="interfaceType"/> is declared as an
    /// <c>@interfaceObject</c> stand-in, read from its <c>@fusion__interfaceObject(schema:)</c>
    /// directives on the merged schema.
    /// </summary>
    internal static IEnumerable<string> GetInterfaceObjectSchemaNames(
        MutableInterfaceTypeDefinition interfaceType)
    {
        foreach (var directive in interfaceType.Directives.AsEnumerable())
        {
            if (directive.Name == WellKnownDirectiveNames.FusionInterfaceObject)
            {
                yield return (string)directive.Arguments[WellKnownArgumentNames.Schema].Value!;
            }
        }
    }

    /// <summary>
    /// Determines whether <paramref name="type"/> implements an interface that has an
    /// <c>@interfaceObject</c> stand-in in <paramref name="schemaName"/>. Such a type can be looked
    /// up in that schema through the stand-in even though the schema does not define the type.
    /// </summary>
    internal static bool ReachesInterfaceObjectStandIn(
        MutableSchemaDefinition schema,
        MutableComplexTypeDefinition type,
        string schemaName)
    {
        foreach (var implementedInterface in type.Implements)
        {
            if (schema.Types.TryGetType<MutableInterfaceTypeDefinition>(
                    implementedInterface.Name,
                    out var interfaceType)
                && GetInterfaceObjectSchemaNames(interfaceType).Contains(schemaName))
            {
                return true;
            }
        }

        return false;
    }

    private static List<IDirective> GetFusionLookupDirectives(
        IDirectivesProvider directivesProvider,
        string? schemaName)
    {
        return directivesProvider.Directives
            .Where(d => d.Name == WellKnownDirectiveNames.FusionLookup
                && (string.IsNullOrEmpty(schemaName)
                    || (string)d.Arguments[WellKnownArgumentNames.Schema].Value! == schemaName))
            .ToList();
    }

    /// <summary>
    /// Returns a list of type names for types that must be preserved in the merged schema
    /// even if they are not directly referenced.
    /// </summary>
    public static HashSet<string> GetPreservedTypeNames(
        ImmutableSortedSet<MutableSchemaDefinition> sourceSchemas)
    {
        var preservedTypeNames = new HashSet<string>();

        foreach (var schema in sourceSchemas)
        {
            foreach (var type in schema.Types.OfType<IObjectTypeDefinition>())
            {
                foreach (var field in type.Fields)
                {
                    foreach (var argument in field.Arguments)
                    {
                        var argumentInnerType = argument.Type.InnerType();

                        if ((argument.HasRequireDirective || field.IsLookup)
                            && argumentInnerType is INameProvider { Name: var name }
                            && !SpecScalarNames.IsSpecScalar(name))
                        {
                            preservedTypeNames.Add(name);
                        }
                    }
                }
            }
        }

        return preservedTypeNames;
    }

    private static void InspectComplexType(
        ISchemaDefinition schema,
        IComplexTypeDefinition complexType,
        Stack<ITypeSystemMember> backlog)
    {
        foreach (var @interface in complexType.Implements)
        {
            backlog.Push(@interface);
        }

        foreach (var directive in complexType.Directives)
        {
            backlog.Push(directive.Definition);
            InspectSelectionCarryingDirective(schema, directive, backlog);
        }

        foreach (var field in complexType.Fields)
        {
            var returnType = field.Type.AsTypeDefinition();
            backlog.Push(returnType);

            foreach (var directive in field.Directives)
            {
                backlog.Push(directive.Definition);
                InspectSelectionCarryingDirective(schema, directive, backlog);
            }

            if (returnType is IInterfaceTypeDefinition or IUnionTypeDefinition)
            {
                foreach (var possibleType in schema.GetPossibleTypes(returnType))
                {
                    backlog.Push(possibleType);
                }
            }

            foreach (var argument in field.Arguments)
            {
                backlog.Push(argument.Type.AsTypeDefinition());

                foreach (var directive in argument.Directives)
                {
                    backlog.Push(directive.Definition);
                    InspectSelectionCarryingDirective(schema, directive, backlog);
                }
            }
        }
    }

    private static void InspectSelectionCarryingDirective(
        ISchemaDefinition schema,
        IDirective directive,
        Stack<ITypeSystemMember> backlog)
    {
        switch (directive.Name)
        {
            case WellKnownDirectiveNames.Key:
            case WellKnownDirectiveNames.Provides:
                if (directive.Arguments.TryGetValue(WellKnownArgumentNames.Fields, out var fieldsValue)
                    && fieldsValue is StringValueNode fieldsSelectionSet)
                {
                    PushSelectionSetReferencedTypes(schema, fieldsSelectionSet.Value, backlog);
                }

                break;

            case WellKnownDirectiveNames.EventStream:
                if (directive.Arguments.TryGetValue(WellKnownArgumentNames.Message, out var messageValue)
                    && messageValue is StringValueNode messageSelectionSet)
                {
                    PushSelectionSetReferencedTypes(schema, messageSelectionSet.Value, backlog);
                }

                break;

            case WellKnownDirectiveNames.Require:
            case WellKnownDirectiveNames.Is:
                if (directive.Arguments.TryGetValue(WellKnownArgumentNames.Field, out var fieldValue)
                    && fieldValue is StringValueNode fieldSelectionMap)
                {
                    PushSelectionMapReferencedTypes(schema, fieldSelectionMap.Value, backlog);
                }

                break;
        }
    }

    private static void PushSelectionSetReferencedTypes(
        ISchemaDefinition schema,
        string selectionSet,
        Stack<ITypeSystemMember> backlog)
    {
        SelectionSetNode parsed;

        // Invalid selections are the validators' concern; skip and continue so the prune
        // never crashes on malformed directive arguments.
        try
        {
            parsed = ParseSelectionSet(selectionSet);
        }
        catch (SyntaxException)
        {
            return;
        }

        CollectSelectionSetTypeConditions(schema, parsed, backlog);
    }

    private static void CollectSelectionSetTypeConditions(
        ISchemaDefinition schema,
        ISyntaxNode node,
        Stack<ITypeSystemMember> backlog)
    {
        if (node is InlineFragmentNode { TypeCondition: { } typeCondition })
        {
            PushTypeByName(schema, typeCondition.Name.Value, backlog);
        }

        foreach (var child in node.GetNodes())
        {
            CollectSelectionSetTypeConditions(schema, child, backlog);
        }
    }

    private static void PushSelectionMapReferencedTypes(
        ISchemaDefinition schema,
        string fieldSelectionMap,
        Stack<ITypeSystemMember> backlog)
    {
        IValueSelectionNode parsed;

        // Invalid maps are the validators' concern; skip and continue so the prune
        // never crashes on malformed directive arguments.
        try
        {
            parsed = new FieldSelectionMapParser(fieldSelectionMap).Parse();
        }
        catch (FieldSelectionMapSyntaxException)
        {
            return;
        }

        CollectSelectionMapTypeConditions(schema, parsed, backlog);
    }

    private static void CollectSelectionMapTypeConditions(
        ISchemaDefinition schema,
        IFieldSelectionMapSyntaxNode node,
        Stack<ITypeSystemMember> backlog)
    {
        switch (node)
        {
            case PathNode { TypeName: { } pathTypeName }:
                PushTypeByName(schema, pathTypeName.Value, backlog);
                break;

            case PathSegmentNode { TypeName: { } segmentTypeName }:
                PushTypeByName(schema, segmentTypeName.Value, backlog);
                break;
        }

        foreach (var child in node.GetNodes())
        {
            CollectSelectionMapTypeConditions(schema, child, backlog);
        }
    }

    private static void PushTypeByName(
        ISchemaDefinition schema,
        string typeName,
        Stack<ITypeSystemMember> backlog)
    {
        if (schema.Types.TryGetType(typeName, out var type))
        {
            backlog.Push(type);
        }
    }

    private static SelectionSetNode ParseSelectionSet(string value)
    {
        try
        {
            return Utf8GraphQLParser.Syntax.ParseSelectionSet(value);
        }
        catch (SyntaxException)
        {
            return Utf8GraphQLParser.Syntax.ParseSelectionSet($"{{ {value} }}");
        }
    }

    private static void InspectDirectiveDefinition(
        IDirectiveDefinition directiveDefinition,
        Stack<ITypeSystemMember> backlog)
    {
        foreach (var argument in directiveDefinition.Arguments)
        {
            backlog.Push(argument.Type.AsTypeDefinition());
        }
    }

    private static void InspectEnumType(
        IEnumTypeDefinition enumType,
        Stack<ITypeSystemMember> backlog)
    {
        foreach (var directive in enumType.Directives)
        {
            backlog.Push(directive.Definition);
        }

        foreach (var value in enumType.Values)
        {
            foreach (var directive in value.Directives)
            {
                backlog.Push(directive.Definition);
            }
        }
    }

    private static void InspectInputObjectType(
        IInputObjectTypeDefinition inputObjectType,
        Stack<ITypeSystemMember> backlog)
    {
        foreach (var directive in inputObjectType.Directives)
        {
            backlog.Push(directive.Definition);
        }

        foreach (var field in inputObjectType.Fields)
        {
            backlog.Push(field.Type.AsTypeDefinition());

            foreach (var directive in field.Directives)
            {
                backlog.Push(directive.Definition);
            }
        }
    }

    private static void InspectUnionType(
        IUnionTypeDefinition unionType,
        Stack<ITypeSystemMember> backlog)
    {
        foreach (var directive in unionType.Directives)
        {
            backlog.Push(directive.Definition);
        }

        foreach (var member in unionType.Types)
        {
            backlog.Push(member);
        }
    }
}
