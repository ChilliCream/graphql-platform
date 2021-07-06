namespace HotChocolate.Resolvers.CodeGeneration
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
        FieldSyntax,
        FieldSelection,
        Context,
        CancellationToken,
        CustomContext,
        DataLoader,
        DirectiveObject,
        EventMessage
    }
}
