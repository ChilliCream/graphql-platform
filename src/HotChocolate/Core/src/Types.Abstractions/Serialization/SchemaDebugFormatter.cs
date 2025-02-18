using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Serialization;

public static class SchemaDebugFormatter
{
    public static ObjectTypeDefinitionNode Format(IObjectTypeDefinition type)
        => new ObjectTypeDefinitionNode(
            null,
            new NameNode(type.Name),
            type.Description is null
                ? null
                : new StringValueNode(type.Description),
            type.Directives.Select(Format).ToArray(),
            type.Implements.Select(FormatTypeRef).Cast<NamedTypeNode>().ToArray(),
            type.Fields.Select(Format).ToArray());

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
        => new DirectiveDefinitionNode(
            null,
            new NameNode(directiveDefinition.Name),
            directiveDefinition.Description is null
                ? null
                : new StringValueNode(directiveDefinition.Description),
            directiveDefinition.IsRepeatable,
            directiveDefinition.Arguments.Select(Format).ToArray(),
            directiveDefinition.Locations.AsEnumerable().Select(Format).ToArray());

    public static FieldDefinitionNode Format(IOutputFieldDefinition field)
        => new FieldDefinitionNode(
            null,
            new NameNode(field.Name),
            field.Description is null
                ? null
                : new StringValueNode(field.Description),
            field.Arguments.Select(Format).ToArray(),
            FormatTypeRef(field.Type),
            field.Directives.Select(Format).ToArray());

    public static InputValueDefinitionNode Format(IInputValueDefinition field)
        => new InputValueDefinitionNode(
            null,
            new NameNode(field.Name),
            field.Description is null
                ? null
                : new StringValueNode(field.Description),
            FormatTypeRef(field.Type),
            field.DefaultValue,
            field.Directives.Select(Format).ToArray());

    public static EnumValueDefinitionNode Format(IEnumValue value)
        => new EnumValueDefinitionNode(
            null,
            new NameNode(value.Name),
            value.Description is null
                ? null
                : new StringValueNode(value.Description),
            value.Directives.Select(Format).ToArray());

    public static DirectiveNode Format(IDirective directive)
        => new DirectiveNode(
            null,
            new NameNode(directive.Definition.Name),
            directive.Arguments.Select(Format).ToArray());

    public static ArgumentNode Format(ArgumentAssignment argument)
        => new ArgumentNode(null, new NameNode(argument.Name), argument.Value);

    private static NameNode Format(Types.DirectiveLocation location)
        => new NameNode(location.Format().ToString());

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
