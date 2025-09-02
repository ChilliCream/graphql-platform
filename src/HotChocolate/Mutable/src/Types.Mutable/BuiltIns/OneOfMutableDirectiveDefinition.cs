namespace HotChocolate.Types.Mutable;

public sealed class OneOfMutableDirectiveDefinition : MutableDirectiveDefinition
{
    internal OneOfMutableDirectiveDefinition()
        : base(BuiltIns.OneOf.Name)
    {
        IsSpecDirective = true;
        Locations = DirectiveLocation.InputObject;
    }
}
