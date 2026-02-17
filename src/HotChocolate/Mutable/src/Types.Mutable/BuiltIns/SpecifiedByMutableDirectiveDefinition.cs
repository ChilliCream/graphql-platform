namespace HotChocolate.Types.Mutable;

public sealed class SpecifiedByMutableDirectiveDefinition : MutableDirectiveDefinition
{
    internal SpecifiedByMutableDirectiveDefinition(MutableScalarTypeDefinition stringType)
        : base(DirectiveNames.SpecifiedBy.Name)
    {
        IsSpecDirective = true;
        Arguments.Add(new MutableInputFieldDefinition(DirectiveNames.SpecifiedBy.Arguments.Url, new NonNullType(stringType)));
        Locations = DirectiveLocation.Scalar;
    }

    public MutableInputFieldDefinition Url => Arguments[DirectiveNames.SpecifiedBy.Arguments.Url];
}
