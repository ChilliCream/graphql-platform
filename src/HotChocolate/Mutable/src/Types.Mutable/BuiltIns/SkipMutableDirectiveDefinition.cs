namespace HotChocolate.Types.Mutable;

public sealed class SkipMutableDirectiveDefinition : MutableDirectiveDefinition
{
    internal SkipMutableDirectiveDefinition(MutableScalarTypeDefinition booleanType)
        : base(BuiltIns.Skip.Name)
    {
        IsSpecDirective = true;
        Arguments.Add(new MutableInputFieldDefinition(BuiltIns.Skip.If, booleanType));
        Locations = DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment;
    }

    public MutableInputFieldDefinition If => Arguments[BuiltIns.Skip.If];
}
