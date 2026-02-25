namespace HotChocolate.Types.Mutable;

public sealed class SkipMutableDirectiveDefinition : MutableDirectiveDefinition
{
    internal SkipMutableDirectiveDefinition(MutableScalarTypeDefinition booleanType)
        : base(DirectiveNames.Skip.Name)
    {
        IsSpecDirective = true;
        Arguments.Add(new MutableInputFieldDefinition(DirectiveNames.Skip.Arguments.If, booleanType));
        Locations = DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment;
    }

    public MutableInputFieldDefinition If => Arguments[DirectiveNames.Skip.Arguments.If];
}
