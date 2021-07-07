namespace HotChocolate.Resolvers.Expressions
{
    internal enum ArgumentKind
    {
        Argument,
        Source,
        Service,
        Schema,
        ObjectType,
        Field,
        DocumentSyntax,
        OperationDefinitionSyntax,
        FieldSyntax,
        Selection,
        Context,
        CancellationToken,
        GlobalState,
        ScopedState,
        LocalState,
        EventMessage,
        Custom
    }
}
