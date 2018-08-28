namespace HotChocolate.Resolvers
{
    public enum FieldResolverArgumentKind
    {
        Argument,
        Source,
        Service,
        Schema,
        ObjectType,
        Field,
        QueryDocument,
        OperationDefinition,
        FieldSelection,
        Context,
        CancellationToken,
        CustomContext,
        DataLoader
    }
}
