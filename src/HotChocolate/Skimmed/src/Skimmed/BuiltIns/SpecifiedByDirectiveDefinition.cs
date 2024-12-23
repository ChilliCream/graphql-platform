using HotChocolate.Types;

namespace HotChocolate.Skimmed;

public sealed class SpecifiedByDirectiveDefinition : DirectiveDefinition
{
    internal SpecifiedByDirectiveDefinition(ScalarTypeDefinition stringType)
        : base(BuiltIns.SpecifiedBy.Name)
    {
        IsSpecDirective = true;
        Arguments.Add(new InputFieldDefinition(BuiltIns.SpecifiedBy.Url, new NonNullTypeDefinition(stringType)));
        Locations = DirectiveLocation.Scalar;
    }

    public InputFieldDefinition Url => Arguments[BuiltIns.SpecifiedBy.Url];
}
