using HotChocolate.Types;

namespace HotChocolate.Skimmed;

public sealed class IncludeDirectiveDefinition : DirectiveDefinition
{
    internal IncludeDirectiveDefinition(ScalarTypeDefinition booleanType)
        : base(BuiltIns.Include.Name)
    {
        IsSpecDirective = true;
        Arguments.Add(new InputFieldDefinition(BuiltIns.Include.If, booleanType));
        Locations = DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment;
    }

    public InputFieldDefinition If => Arguments[BuiltIns.Include.If];
}
