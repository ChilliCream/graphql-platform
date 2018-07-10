namespace HotChocolate.Resolvers
{
    public enum FieldResolverKind
    {
        // the resolver is a FieldResolverDelegate
        Delegate,

        // the resolver is embeded in the source object => ctx.Parent()
        Source,

        // the resolver is embeded in a class that represents a collection of resolvers.
        Collection
    }
}