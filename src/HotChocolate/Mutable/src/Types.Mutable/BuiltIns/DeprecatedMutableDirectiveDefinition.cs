namespace HotChocolate.Types.Mutable;

public sealed class DeprecatedMutableDirectiveDefinition : MutableDirectiveDefinition
{
    internal DeprecatedMutableDirectiveDefinition(MutableScalarTypeDefinition stringType)
        : base(BuiltIns.Deprecated.Name)
    {
        IsSpecDirective = true;
        Arguments.Add(new MutableInputFieldDefinition(BuiltIns.Deprecated.Reason, stringType));
        Locations = DirectiveLocation.FieldDefinition
            | DirectiveLocation.ArgumentDefinition
            | DirectiveLocation.InputFieldDefinition
            | DirectiveLocation.EnumValue;
    }

    public MutableInputFieldDefinition Reason => Arguments[BuiltIns.Deprecated.Reason];
}
