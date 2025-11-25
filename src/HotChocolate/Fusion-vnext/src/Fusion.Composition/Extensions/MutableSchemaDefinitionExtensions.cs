using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

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
        if (!string.IsNullOrEmpty(schemaName) && !type.ExistsInSchema(schemaName))
        {
            return [];
        }

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

        // Get the lookups of unions this type is a member of,
        // if it's an object type.
        if (type.Kind == TypeKind.Object)
        {
            var unionTypes = schema.Types
                .OfType<MutableUnionTypeDefinition>()
                .Where(u => u.Types.Contains(type))
                .ToImmutableArray();

            foreach (var unionType in unionTypes)
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

    public static void RemoveUnreferencedDefinitions(
        this MutableSchemaDefinition schema,
        HashSet<string> preserveInputTypeNames)
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

        while (backlog.TryPop(out var type))
        {
            if (!touchedDefinitions.Add(type)
                || type is ITypeDefinition { Kind: TypeKind.Scalar or TypeKind.Enum })
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

                case IInputObjectTypeDefinition inputObjectType:
                    InspectInputObjectType(inputObjectType, backlog);
                    break;

                case IUnionTypeDefinition unionType:
                    InspectUnionType(unionType, backlog);
                    break;
            }
        }

        var definitionsToRemove = new HashSet<ITypeSystemMember>();
        foreach (var type in schema.Types)
        {
            if (touchedDefinitions.Contains(type) || preserveInputTypeNames.Contains(type.NamedType().Name))
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
        }

        foreach (var field in complexType.Fields)
        {
            var returnType = field.Type.AsTypeDefinition();
            backlog.Push(returnType);

            foreach (var directive in field.Directives)
            {
                backlog.Push(directive.Definition);
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
                }
            }
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
