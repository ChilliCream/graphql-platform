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
        FieldSelection,
        Context,
        CancellationToken,
        CustomContext,
        DataLoader,
        ResolverResult,
        Directive,
        DirectiveArgument,
        DirectiveObject
    }
}
