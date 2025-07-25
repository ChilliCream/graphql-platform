using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Extensions;

internal static class MutableSchemaDefinitionExtensions
{
    public static void AddBuiltInFusionTypes(this MutableSchemaDefinition schema)
    {
        foreach (var builtInScalar in FusionBuiltIns.SourceSchemaScalars)
        {
            schema.Types.Add(builtInScalar);
        }
    }

    public static void AddBuiltInFusionDirectives(this MutableSchemaDefinition schema)
    {
        foreach (var builtInDirective in FusionBuiltIns.SourceSchemaDirectives)
        {
            schema.DirectiveDefinitions.Add(builtInDirective);
        }
    }

    public static bool IsRootOperationType(
        this MutableSchemaDefinition schema,
        MutableObjectTypeDefinition type)
    {
        return
            schema.QueryType == type
            || schema.MutationType == type
            || schema.SubscriptionType == type;
    }

    public static void RemoveUnreferencedTypes(this MutableSchemaDefinition schema)
    {
        var touchedTypes = new HashSet<ITypeDefinition>();
        var backlog = new Stack<ITypeDefinition>();

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
            if (!touchedTypes.Add(type)
                || type.Kind == TypeKind.Scalar
                || type.Kind == TypeKind.Enum)
            {
                continue;
            }

            switch (type)
            {
                case IComplexTypeDefinition complexType:
                    InspectComplexType(schema, complexType, backlog);
                    break;

                case IInputObjectTypeDefinition inputObjectType:
                    InspectInputObjectType(inputObjectType, backlog);
                    break;

                case IUnionTypeDefinition unionType:
                    InspectUnionType(unionType, backlog);
                    break;
            }
        }

        var typesToRemove = new HashSet<ITypeDefinition>();
        foreach (var type in schema.Types)
        {
            if (touchedTypes.Contains(type))
            {
                continue;
            }

            typesToRemove.Add(type);
        }

        if (typesToRemove.Count > 0)
        {
            foreach (var type in typesToRemove)
            {
                schema.Types.Remove(type);
            }
        }
    }

    private static void InspectComplexType(
        ISchemaDefinition schema,
        IComplexTypeDefinition complexType,
        Stack<ITypeDefinition> backlog)
    {
        foreach (var @interface in complexType.Implements)
        {
            backlog.Push(@interface);
        }

        foreach (var field in complexType.Fields)
        {
            var returnType = field.Type.AsTypeDefinition();
            backlog.Push(returnType);

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
            }
        }
    }

    private static void InspectInputObjectType(
        IInputObjectTypeDefinition inputObjectType,
        Stack<ITypeDefinition> backlog)
    {
        foreach (var field in inputObjectType.Fields)
        {
            backlog.Push(field.Type.AsTypeDefinition());
        }
    }

    private static void InspectUnionType(
        IUnionTypeDefinition unionType,
        Stack<ITypeDefinition> backlog)
    {
        foreach (var member in unionType.Types)
        {
            backlog.Push(member);
        }
    }
}
