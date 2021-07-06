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
        FieldSelection,
        Context,
        CancellationToken,
        GlobalState,
        ScopedState,
        LocalState,
        EventMessage,
        Custom
    }
}
