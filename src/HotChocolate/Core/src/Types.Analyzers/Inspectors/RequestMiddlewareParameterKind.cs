namespace HotChocolate.Types.Analyzers.Inspectors;

public enum RequestMiddlewareParameterKind
{
    Service,
    SchemaService,
    SchemaName,
    Schema,
    Context,
    Next
}