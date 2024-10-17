namespace HotChocolate.Skimmed;

public sealed class DeprecatedDirectiveDefinition : DirectiveDefinition
{
    internal DeprecatedDirectiveDefinition(StringTypeDefinition stringType)
        : base(BuiltIns.Deprecated.Name)
    {
        IsSpecDirective = true;
        Arguments.Add(new InputFieldDefinition(BuiltIns.Deprecated.Reason, stringType));
    }

    public InputFieldDefinition Reason => Arguments[BuiltIns.Deprecated.Reason];
}
