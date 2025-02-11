namespace HotChocolate.Types.Mutable;

public sealed class SpecifiedByMutableDirectiveDefinition : MutableDirectiveDefinition
{
    internal SpecifiedByMutableDirectiveDefinition(ScalarTypeDefinition stringType)
        : base(BuiltIns.SpecifiedBy.Name)
    {
        IsSpecDirective = true;
        Arguments.Add(new MutableInputFieldDefinition(BuiltIns.SpecifiedBy.Url, new NonNullTypeDefinition(stringType)));
        Locations = DirectiveLocation.Scalar;
    }

    public MutableInputFieldDefinition Url => Arguments[BuiltIns.SpecifiedBy.Url];
}
