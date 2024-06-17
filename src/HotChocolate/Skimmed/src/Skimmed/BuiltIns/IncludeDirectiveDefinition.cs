namespace HotChocolate.Skimmed;

public sealed class IncludeDirectiveDefinition : DirectiveDefinition
{
    internal IncludeDirectiveDefinition(BooleanTypeDefinition booleanType)
        : base(BuiltIns.Include.Name)
    {
        IsSpecScalar = true;
        Arguments.Add(new InputFieldDefinition(BuiltIns.Include.If, booleanType));
    }

    public InputFieldDefinition If => Arguments[BuiltIns.Include.If];
}
