namespace HotChocolate.Configuration
{
    // applies the bindings and creates a FieldResolverObjects from the specified binding object
    // the FieldResolverObject contains the actual field resolver delegate.
    internal interface IResolverBindingHandler
    {
        // TODO : Create an object ResolverBinding that shall be passed in which extends more guaranties.
        void ApplyBinding(
            ISchemaContext schemaContext,
            ResolverBindingInfo resolverBindingInfo);
    }
}
