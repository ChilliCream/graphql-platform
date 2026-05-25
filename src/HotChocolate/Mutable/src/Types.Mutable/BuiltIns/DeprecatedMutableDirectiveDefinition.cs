namespace HotChocolate.Types.Mutable;

public sealed class DeprecatedMutableDirectiveDefinition : MutableDirectiveDefinition
{
    internal DeprecatedMutableDirectiveDefinition(MutableScalarTypeDefinition stringType)
        : base(DirectiveNames.Deprecated.Name)
    {
        IsSpecDirective = true;
        Arguments.Add(new MutableInputFieldDefinition(DirectiveNames.Deprecated.Arguments.Reason, stringType));
        Locations = DirectiveLocation.FieldDefinition
            | DirectiveLocation.ArgumentDefinition
            | DirectiveLocation.InputFieldDefinition
            | DirectiveLocation.EnumValue;
    }

    public MutableInputFieldDefinition Reason => Arguments[DirectiveNames.Deprecated.Arguments.Reason];
}
