namespace HotChocolate.Internal;

/// <summary>
/// Specifies resolver parameter kind.
/// </summary>
public enum ArgumentKind
{
    Argument,
    Source,
    Service,
    Schema,
    ObjectType,
    Field,
    DocumentSyntax,
    OperationDefinitionSyntax,
    Operation,
    FieldSyntax,
    Selection,
    Context,
    CancellationToken,
    GlobalState,
    ScopedState,
    LocalState,
    EventMessage,
    Custom,
}
