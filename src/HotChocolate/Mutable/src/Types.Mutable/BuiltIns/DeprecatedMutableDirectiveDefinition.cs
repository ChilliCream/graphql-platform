using HotChocolate.Language;

namespace HotChocolate.Types.Mutable;

public sealed class DeprecatedMutableDirectiveDefinition : MutableDirectiveDefinition
{
    internal DeprecatedMutableDirectiveDefinition(MutableScalarTypeDefinition stringType)
        : base(DirectiveNames.Deprecated.Name)
    {
        IsSpecDirective = true;
        Arguments.Add(
            new MutableInputFieldDefinition(
                DirectiveNames.Deprecated.Arguments.Reason,
                new NonNullType(stringType))
            {
                DefaultValue = new StringValueNode(
                    DirectiveNames.Deprecated.Arguments.DefaultReason)
            });
        Locations = DirectiveLocation.FieldDefinition
            | DirectiveLocation.ArgumentDefinition
            | DirectiveLocation.InputFieldDefinition
            | DirectiveLocation.EnumValue
            | DirectiveLocation.DirectiveDefinition
            | DirectiveLocation.Object;
    }

    public MutableInputFieldDefinition Reason => Arguments[DirectiveNames.Deprecated.Arguments.Reason];
}
