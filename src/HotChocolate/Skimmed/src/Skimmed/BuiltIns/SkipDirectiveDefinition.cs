namespace HotChocolate.Skimmed;

public sealed class SkipDirectiveDefinition : DirectiveDefinition
{
    internal SkipDirectiveDefinition(BooleanTypeDefinition booleanType)
        : base(BuiltIns.Skip.Name)
    {
        IsSpecScalar = true;
        Arguments.Add(new InputFieldDefinition(BuiltIns.Skip.If, booleanType));
    }

    public InputFieldDefinition If => Arguments[BuiltIns.Skip.If];
}
