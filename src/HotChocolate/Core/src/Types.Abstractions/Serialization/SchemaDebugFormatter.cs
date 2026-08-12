using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Serialization;

public static class SchemaDebugFormatter
{
    public static ObjectTypeDefinitionNode Format(IObjectTypeDefinition type)
    {
        var directives = type.Directives.Select(Format).ToList();

        ApplyDeprecatedDirective(type, directives);

        return new ObjectTypeDefinitionNode(
            null,
            new NameNode(type.Name),
            type.Description is null
                ? null
                : new StringValueNode(type.Description),
            directives,
            type.Implements.Select(FormatTypeRef).Cast<NamedTypeNode>().ToArray(),
            type.Fields.Where(t => !t.IsIntrospectionField).Select(Format).ToArray());
    }

    public static InterfaceTypeDefinitionNode Format(IInterfaceTypeDefinition type)
        => new InterfaceTypeDefinitionNode(
            null,
            new NameNode(type.Name),
            type.Description is null
                ? null
                : new StringValueNode(type.Description),
            type.Directives.Select(Format).ToArray(),
            type.Implements.Select(FormatTypeRef).Cast<NamedTypeNode>().ToArray(),
            type.Fields.Select(Format).ToArray());

    public static UnionTypeDefinitionNode Format(IUnionTypeDefinition type)
        => new UnionTypeDefinitionNode(
            null,
            new NameNode(type.Name),
            type.Description is null
                ? null
                : new StringValueNode(type.Description),
            type.Directives.Select(Format).ToArray(),
            type.Types.Select(FormatTypeRef).Cast<NamedTypeNode>().ToArray());

    public static InputObjectTypeDefinitionNode Format(IInputObjectTypeDefinition type)
        => new InputObjectTypeDefinitionNode(
            null,
            new NameNode(type.Name),
            type.Description is null
                ? null
                : new StringValueNode(type.Description),
            type.Directives.Select(Format).ToArray(),
            type.Fields.Select(Format).ToArray());

    public static EnumTypeDefinitionNode Format(IEnumTypeDefinition type)
            => new EnumTypeDefinitionNode(
            null,
            new NameNode(type.Name),
            type.Description is null
                ? null
                : new StringValueNode(type.Description),
            type.Directives.Select(Format).ToArray(),
            type.Values.Select(Format).ToArray());

    public static ScalarTypeDefinitionNode Format(IScalarTypeDefinition type)
        => new ScalarTypeDefinitionNode(
            null,
            new NameNode(type.Name),
            type.Description is null
                ? null
                : new StringValueNode(type.Description),
            type.Directives.Select(Format).ToArray());

    public static DirectiveDefinitionNode Format(IDirectiveDefinition directiveDefinition)
    {
        var directives = directiveDefinition.Directives.Select(Format).ToList();

        ApplyDeprecatedDirective(directiveDefinition, directives);

        return new DirectiveDefinitionNode(
            null,
            new NameNode(directiveDefinition.Name),
            directiveDefinition.Description is null
                ? null
                : new StringValueNode(directiveDefinition.Description),
            directiveDefinition.IsRepeatable,
            directiveDefinition.Arguments.Select(Format).ToArray(),
            directives,
            DirectiveLocationUtils.AsEnumerable(directiveDefinition.Locations).Select(Format).ToArray());
    }

    public static FieldDefinitionNode Format(IOutputFieldDefinition field)
    {
        var directives = field.Directives.Select(Format).ToList();

        ApplyDeprecatedDirective(field, directives);

        return new FieldDefinitionNode(
            null,
            new NameNode(field.Name),
            field.Description is null
                ? null
                : new StringValueNode(field.Description),
            field.Arguments.Select(Format).ToArray(),
            FormatTypeRef(field.Type),
            directives);
    }

    public static InputValueDefinitionNode Format(IInputValueDefinition field)
    {
        var directives = field.Directives.Select(Format).ToList();

        ApplyDeprecatedDirective(field, directives);

        return new InputValueDefinitionNode(
            null,
            new NameNode(field.Name),
            field.Description is null
                ? null
                : new StringValueNode(field.Description),
            FormatTypeRef(field.Type),
            field.DefaultValue,
            directives);
    }

    public static EnumValueDefinitionNode Format(IEnumValue value)
    {
        var directives = value.Directives.Select(Format).ToList();

        ApplyDeprecatedDirective(value, directives);

        return new EnumValueDefinitionNode(
            null,
            new NameNode(value.Name),
            value.Description is null
                ? null
                : new StringValueNode(value.Description),
            directives);
    }

    public static DirectiveNode Format(IDirective directive)
        => new DirectiveNode(
            null,
            new NameNode(directive.Definition.Name),
            directive.Arguments.Select(Format).ToArray());

    public static ArgumentNode Format(ArgumentAssignment argument)
        => new ArgumentNode(null, new NameNode(argument.Name), argument.Value);

    private static void ApplyDeprecatedDirective(
        IDeprecationProvider canBeDeprecated,
        List<DirectiveNode> directives)
    {
        if (!canBeDeprecated.IsDeprecated)
        {
            return;
        }

        var index = directives.FindIndex(t => t.Name.Value == DirectiveNames.Deprecated.Name);

        if (index == -1)
        {
            directives.Insert(0, CreateDeprecatedDirective(canBeDeprecated));
            return;
        }

        if (canBeDeprecated.HasDefaultDeprecationReason && directives[index].Arguments.Count > 0)
        {
            directives[index] = CreateDeprecatedDirective(canBeDeprecated);
        }
    }

    internal static DirectiveNode CreateDeprecatedDirective(IDeprecationProvider canBeDeprecated)
        => canBeDeprecated.HasDefaultDeprecationReason
            ? new DirectiveNode(DirectiveNames.Deprecated.Name)
            : new DirectiveNode(
                DirectiveNames.Deprecated.Name,
                new ArgumentNode(
                    DirectiveNames.Deprecated.Arguments.Reason,
                    canBeDeprecated.DeprecationReason!));

    private static NameNode Format(Types.DirectiveLocation location)
        => new(location.Format().ToString());

    public static ITypeNode FormatTypeRef(IType type)
    {
        switch (type.Kind)
        {
            case TypeKind.List:
                return new ListTypeNode(FormatTypeRef(((ListType)type).ElementType));

            case TypeKind.NonNull:
                return new NonNullTypeNode(
                    (INullableTypeNode)FormatTypeRef(
                        ((NonNullType)type).NullableType));

            default:
                return new NamedTypeNode(((ITypeDefinition)type).Name);
        }
    }
}
