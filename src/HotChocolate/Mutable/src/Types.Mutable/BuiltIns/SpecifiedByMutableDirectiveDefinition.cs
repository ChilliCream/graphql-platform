namespace HotChocolate.Types.Mutable;

public sealed class SpecifiedByMutableDirectiveDefinition : MutableDirectiveDefinition
{
    internal SpecifiedByMutableDirectiveDefinition(MutableScalarTypeDefinition stringType)
        : base(BuiltIns.SpecifiedBy.Name)
    {
        IsSpecDirective = true;
        Arguments.Add(new MutableInputFieldDefinition(BuiltIns.SpecifiedBy.Url, new NonNullType(stringType)));
        Locations = DirectiveLocation.Scalar;
    }

    public MutableInputFieldDefinition Url => Arguments[BuiltIns.SpecifiedBy.Url];
}
