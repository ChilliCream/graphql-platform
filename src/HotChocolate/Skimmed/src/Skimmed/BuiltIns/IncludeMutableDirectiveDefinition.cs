namespace HotChocolate.Types.Mutable;

public sealed class IncludeMutableDirectiveDefinition : MutableDirectiveDefinition
{
    internal IncludeMutableDirectiveDefinition(ScalarTypeDefinition booleanType)
        : base(BuiltIns.Include.Name)
    {
        IsSpecDirective = true;
        Arguments.Add(new MutableInputFieldDefinition(BuiltIns.Include.If, booleanType));
        Locations = DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment;
    }

    public MutableInputFieldDefinition If => Arguments[BuiltIns.Include.If];
}
