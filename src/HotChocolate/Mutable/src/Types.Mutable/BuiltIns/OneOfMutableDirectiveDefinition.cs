namespace HotChocolate.Types.Mutable;

public sealed class OneOfMutableDirectiveDefinition : MutableDirectiveDefinition
{
    internal OneOfMutableDirectiveDefinition()
        : base(DirectiveNames.OneOf.Name)
    {
        IsSpecDirective = true;
        Locations = DirectiveLocation.InputObject;
    }

    public static OneOfMutableDirectiveDefinition Create(ISchemaDefinition _)
    {
        return new OneOfMutableDirectiveDefinition();
    }
}
