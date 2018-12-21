namespace HotChocolate.Execution.Configuration
{
    public interface IQueryExecutionOptionsAccessor
        : IRequestTimeoutOptionsAccessor
        , IValidateQueryOptionsAccessor
        , IErrorHandlerOptionsAccessor
    { }
}
