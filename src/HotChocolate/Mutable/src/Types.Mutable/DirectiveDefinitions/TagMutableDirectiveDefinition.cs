namespace HotChocolate.Types.Mutable.DirectiveDefinitions;

public sealed class TagMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public TagMutableDirectiveDefinition(MutableScalarTypeDefinition stringType)
        : base(WellKnownDirectiveNames.Tag)
    {
        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Name,
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
}
