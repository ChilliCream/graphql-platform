using HotChocolate.Types;

namespace HotChocolate.Skimmed;

public sealed class DeprecatedDirectiveDefinition : DirectiveDefinition
{
    internal DeprecatedDirectiveDefinition(ScalarTypeDefinition stringType)
        : base(BuiltIns.Deprecated.Name)
    {
        IsSpecDirective = true;
        Arguments.Add(new InputFieldDefinition(BuiltIns.Deprecated.Reason, stringType));
        Locations = DirectiveLocation.FieldDefinition |
            DirectiveLocation.ArgumentDefinition |
            DirectiveLocation.InputFieldDefinition |
            DirectiveLocation.EnumValue;
    }

    public InputFieldDefinition Reason => Arguments[BuiltIns.Deprecated.Reason];
}
