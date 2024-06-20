namespace HotChocolate.Types.Analyzers.Models;

public enum ResolverParameterKind
{
    Unknown,
    Parent,
    CancellationToken,
    ClaimsPrincipal,
    DocumentNode,
    EventMessage,
    FieldNode,
    OutputField,
    HttpContext,
    HttpRequest,
    HttpResponse,
    GetGlobalState,
    SetGlobalState,
    GetScopedState,
    SetScopedState,
    GetLocalState,
    SetLocalState,
    Service,
    Argument
}
