namespace HotChocolate.Types.Mutable;

public sealed class OneOfMutableDirectiveDefinition : MutableDirectiveDefinition
{
    internal OneOfMutableDirectiveDefinition()
        : base(BuiltIns.OneOf.Name)
    {
        IsSpecDirective = true;
        Description = "The `@oneOf` directive is used within the type system definition language to indicate that an Input Object is a OneOf Input Object.";
        Locations = DirectiveLocation.InputObject;
    }

    public static OneOfMutableDirectiveDefinition Create(ISchemaDefinition schema)
    {
        return new OneOfMutableDirectiveDefinition();
    }
}
