namespace HotChocolate.Resolvers
{
    internal enum ArgumentKind
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
