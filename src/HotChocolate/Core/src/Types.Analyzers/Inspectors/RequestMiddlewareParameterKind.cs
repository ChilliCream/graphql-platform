namespace HotChocolate.Types.Analyzers.Inspectors;

public enum RequestMiddlewareParameterKind
{
    Service,
    SchemaService,
    Schema,
    Context,
    Next,
}