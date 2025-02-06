using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Serialization;

internal static class SchemaDebugFormatter
{
    public static ObjectTypeDefinitionNode RewriteObjectType(IReadOnlyObjectTypeDefinition type)
        => new ObjectTypeDefinitionNode(
            null,
            new NameNode(type.Name),
            type.Description is null
                ? null
                : new StringValueNode(type.Description),
            type.Directives.Select(RewriteDirective).ToArray(),
            type.Implements.Select(RewriteTypeRef).Cast<NamedTypeNode>().ToArray(),
            type.Fields.Select(RewriteOutputField).ToArray());

    public static InterfaceTypeDefinitionNode RewriteInterfaceType(IReadOnlyInterfaceTypeDefinition type)
        => new InterfaceTypeDefinitionNode(
            null,
            new NameNode(type.Name),
            type.Description is null
                ? null
                : new StringValueNode(type.Description),
            type.Directives.Select(RewriteDirective).ToArray(),
            type.Implements.Select(RewriteTypeRef).Cast<NamedTypeNode>().ToArray(),
            type.Fields.Select(RewriteOutputField).ToArray());

    public static UnionTypeDefinitionNode RewriteUnionType(IReadOnlyUnionTypeDefinition type)
        => new UnionTypeDefinitionNode(
            null,
            new NameNode(type.Name),
            type.Description is null
                ? null
                : new StringValueNode(type.Description),
            type.Directives.Select(RewriteDirective).ToArray(),
            type.Types.Select(RewriteTypeRef).Cast<NamedTypeNode>().ToArray());

    public static InputObjectTypeDefinitionNode RewriteInputObjectType(IReadOnlyInputObjectTypeDefinition type)
        => new InputObjectTypeDefinitionNode(
            null,
            new NameNode(type.Name),
            type.Description is null
                ? null
                : new StringValueNode(type.Description),
            type.Directives.Select(RewriteDirective).ToArray(),
            type.Fields.Select(RewriteInputField).ToArray());

    public static EnumTypeDefinitionNode RewriteEnumType(IReadOnlyEnumTypeDefinition type)
            => new EnumTypeDefinitionNode(
            null,
            new NameNode(type.Name),
            type.Description is null
                ? null
                : new StringValueNode(type.Description),
            type.Directives.Select(RewriteDirective).ToArray(),
            type.Values.Select(RewriteEnumValue).ToArray());

    public static ScalarTypeDefinitionNode RewriteScalarType(IReadOnlyScalarTypeDefinition type)
        => new ScalarTypeDefinitionNode(
            null,
            new NameNode(type.Name),
            type.Description is null
                ? null
                : new StringValueNode(type.Description),
            type.Directives.Select(RewriteDirective).ToArray());

    public static DirectiveDefinitionNode RewriteDirectiveType(IReadOnlyDirectiveDefinition directiveDefinition)
        => new DirectiveDefinitionNode(
            null,
            new NameNode(directiveDefinition.Name),
            directiveDefinition.Description is null
                ? null
                : new StringValueNode(directiveDefinition.Description),
            directiveDefinition.IsRepeatable,
            directiveDefinition.Arguments.Select(RewriteInputField).ToArray(),
            directiveDefinition.Locations.AsEnumerable().Select(RewriteDirectiveLocation).ToArray());

    public static FieldDefinitionNode RewriteOutputField(IReadOnlyOutputFieldDefinition field)
        => new FieldDefinitionNode(
            null,
            new NameNode(field.Name),
            field.Description is null
                ? null
                : new StringValueNode(field.Description),
            field.Arguments.Select(RewriteInputField).ToArray(),
            RewriteTypeRef(field.Type),
            field.Directives.Select(RewriteDirective).ToArray());

    public static InputValueDefinitionNode RewriteInputField(IReadOnlyInputValueDefinition field)
        => new InputValueDefinitionNode(
            null,
            new NameNode(field.Name),
            field.Description is null
                ? null
                : new StringValueNode(field.Description),
            RewriteTypeRef(field.Type),
            field.DefaultValue,
            field.Directives.Select(RewriteDirective).ToArray());

    public static EnumValueDefinitionNode RewriteEnumValue(IReadOnlyEnumValue value)
        => new EnumValueDefinitionNode(
            null,
            new NameNode(value.Name),
            value.Description is null
                ? null
                : new StringValueNode(value.Description),
            value.Directives.Select(RewriteDirective).ToArray());

    public static DirectiveNode RewriteDirective(IReadOnlyDirective directive)
        => new DirectiveNode(
            null,
            new NameNode(directive.Definition.Name),
            directive.Arguments.Select(RewriteArgument).ToArray());

    public static ArgumentNode RewriteArgument(ArgumentAssignment argument)
        => new ArgumentNode(null, new NameNode(argument.Name), argument.Value);

    private static NameNode RewriteDirectiveLocation(Types.DirectiveLocation location)
        => new NameNode(location.Format().ToString());

    public static ITypeNode RewriteTypeRef(IReadOnlyTypeDefinition type)
    {
        switch (type.Kind)
        {
            case TypeKind.List:
                return new ListTypeNode(RewriteTypeRef(((IReadOnlyWrapperType)type).Type));

            case TypeKind.NonNull:
                return new NonNullTypeNode(
                    (INullableTypeNode)RewriteTypeRef(
                        ((IReadOnlyWrapperType)type).Type));

            default:
                return new NamedTypeNode(((IReadOnlyNamedTypeDefinition)type).Name);
        }
    }
}


