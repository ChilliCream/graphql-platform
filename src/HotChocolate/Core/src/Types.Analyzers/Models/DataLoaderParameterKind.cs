namespace HotChocolate.Types.Analyzers.Models;

public enum DataLoaderParameterKind
{
    Key = 0,
    Service = 1,
    ContextData = 2,
    CancellationToken = 3,
    SelectorBuilder = 4,
    PagingArguments = 5,
    PredicateBuilder = 6
}
