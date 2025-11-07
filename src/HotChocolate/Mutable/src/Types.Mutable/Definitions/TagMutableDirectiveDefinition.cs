namespace HotChocolate.Types.Mutable.Definitions;

public sealed class TagMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public TagMutableDirectiveDefinition(MutableScalarTypeDefinition stringType)
        : base(DirectiveNames.Tag.Name)
    {
        Arguments.Add(
            new MutableInputFieldDefinition(
                DirectiveNames.Tag.Arguments.Name,
                new NonNullType(stringType)));
        IsRepeatable = true;
        Locations =
            DirectiveLocation.Object
            | DirectiveLocation.Interface
            | DirectiveLocation.Union
            | DirectiveLocation.InputObject
            | DirectiveLocation.Enum
            | DirectiveLocation.Scalar
            | DirectiveLocation.FieldDefinition
            | DirectiveLocation.InputFieldDefinition
            | DirectiveLocation.ArgumentDefinition
            | DirectiveLocation.EnumValue
            | DirectiveLocation.Schema;
    }

    public static TagMutableDirectiveDefinition Create(ISchemaDefinition schema)
    {
        if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(SpecScalarNames.String.Name, out var stringType))
        {
            stringType = BuiltIns.String.Create();
        }

        return new TagMutableDirectiveDefinition(stringType);
    }
}
