using HotChocolate.Types;

namespace HotChocolate.Skimmed;

public sealed class SkipDirectiveDefinition : DirectiveDefinition
{
    internal SkipDirectiveDefinition(ScalarTypeDefinition booleanType)
        : base(BuiltIns.Skip.Name)
    {
        IsSpecDirective = true;
        Arguments.Add(new InputFieldDefinition(BuiltIns.Skip.If, booleanType));
        Locations = DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment;
    }

    public InputFieldDefinition If => Arguments[BuiltIns.Skip.If];
}
