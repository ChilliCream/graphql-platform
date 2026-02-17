namespace HotChocolate.Types.Mutable;

public sealed class IncludeMutableDirectiveDefinition : MutableDirectiveDefinition
{
    internal IncludeMutableDirectiveDefinition(MutableScalarTypeDefinition booleanType)
        : base(DirectiveNames.Include.Name)
    {
        IsSpecDirective = true;
        Arguments.Add(new MutableInputFieldDefinition(DirectiveNames.Include.Arguments.If, booleanType));
        Locations = DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment;
    }

    public MutableInputFieldDefinition If => Arguments[DirectiveNames.Include.Arguments.If];
}
