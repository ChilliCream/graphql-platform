using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Skimmed.Serialization;

internal static class SchemaDebugFormatter
{
    public static ObjectTypeDefinitionNode RewriteObjectType(ObjectTypeDefinition type)
        => new ObjectTypeDefinitionNode(
            null,
            new NameNode(type.Name),
            type.Description is null
                ? null
                : new StringValueNode(type.Description),
            type.Directives.Select(RewriteDirective).ToArray(),
            type.Implements.Select(RewriteTypeRef).Cast<NamedTypeNode>().ToArray(),
            type.Fields.Select(RewriteOutputField).ToArray());

    public static InterfaceTypeDefinitionNode RewriteInterfaceType(InterfaceTypeDefinition type)
        => new InterfaceTypeDefinitionNode(
            null,
            new NameNode(type.Name),
            type.Description is null
                ? null
                : new StringValueNode(type.Description),
            type.Directives.Select(RewriteDirective).ToArray(),
            type.Implements.Select(RewriteTypeRef).Cast<NamedTypeNode>().ToArray(),
            type.Fields.Select(RewriteOutputField).ToArray());

    public static UnionTypeDefinitionNode RewriteUnionType(UnionTypeDefinition type)
        => new UnionTypeDefinitionNode(
            null,
            new NameNode(type.Name),
            type.Description is null
                ? null
                : new StringValueNode(type.Description),
            type.Directives.Select(RewriteDirective).ToArray(),
            type.Types.Select(RewriteTypeRef).Cast<NamedTypeNode>().ToArray());

    public static InputObjectTypeDefinitionNode RewriteInputObjectType(InputObjectTypeDefinition type)
        => new InputObjectTypeDefinitionNode(
            null,
            new NameNode(type.Name),
            type.Description is null
                ? null
                : new StringValueNode(type.Description),
            type.Directives.Select(RewriteDirective).ToArray(),
            type.Fields.Select(RewriteInputField).ToArray());

    public static EnumTypeDefinitionNode RewriteEnumType(EnumTypeDefinition type)
            => new EnumTypeDefinitionNode(
            null,
            new NameNode(type.Name),
            type.Description is null
                ? null
                : new StringValueNode(type.Description),
            type.Directives.Select(RewriteDirective).ToArray(),
            type.Values.Select(RewriteEnumValue).ToArray());

    public static ScalarTypeDefinitionNode RewriteScalarType(ScalarTypeDefinition type)
        => new ScalarTypeDefinitionNode(
            null,
            new NameNode(type.Name),
            type.Description is null
                ? null
                : new StringValueNode(type.Description),
            type.Directives.Select(RewriteDirective).ToArray());

    public static DirectiveDefinitionNode RewriteDirectiveType(DirectiveDefinition type)
        => new DirectiveDefinitionNode(
            null,
            new NameNode(type.Name),
            type.Description is null
                ? null
                : new StringValueNode(type.Description),
            type.IsRepeatable,
            type.Arguments.Select(RewriteInputField).ToArray(),
            type.Locations.AsEnumerable().Select(RewriteDirectiveLocation).ToArray());

    public static FieldDefinitionNode RewriteOutputField(OutputFieldDefinition field)
        => new FieldDefinitionNode(
            null,
            new NameNode(field.Name),
            field.Description is null
                ? null
                : new StringValueNode(field.Description),
            field.Arguments.Select(RewriteInputField).ToArray(),
            RewriteTypeRef(field.Type),
            field.Directives.Select(RewriteDirective).ToArray());

    public static InputValueDefinitionNode RewriteInputField(InputFieldDefinition field)
        => new InputValueDefinitionNode(
            null,
            new NameNode(field.Name),
            field.Description is null
                ? null
                : new StringValueNode(field.Description),
            RewriteTypeRef(field.Type),
            field.DefaultValue,
            field.Directives.Select(RewriteDirective).ToArray());

    public static EnumValueDefinitionNode RewriteEnumValue(EnumValue value)
        => new EnumValueDefinitionNode(
            null,
            new NameNode(value.Name),
            value.Description is null
                ? null
                : new StringValueNode(value.Description),
            value.Directives.Select(RewriteDirective).ToArray());

    public static DirectiveNode RewriteDirective(Directive directive)
        => new DirectiveNode(
            null,
            new NameNode(directive.Name),
            directive.Arguments.Select(RewriteArgument).ToArray());

    public static ArgumentNode RewriteArgument(ArgumentAssignment argument)
        => new ArgumentNode(null, new NameNode(argument.Name), argument.Value);

    private static NameNode RewriteDirectiveLocation(Types.DirectiveLocation location)
        => new NameNode(location.ToString());

    public static ITypeNode RewriteTypeRef(ITypeDefinition type)
    {
        switch (type.Kind)
        {
            case TypeKind.List:
                return new ListTypeNode(RewriteTypeRef(((ListTypeDefinition)type).ElementType));

            case TypeKind.NonNull:
                return new NonNullTypeNode(
                    (INullableTypeNode)RewriteTypeRef(
                        ((NonNullTypeDefinition)type).NullableType));

            default:
                return new NamedTypeNode(((INamedTypeDefinition)type).Name);
        }
    }
}
