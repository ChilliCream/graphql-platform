namespace HotChocolate.Execution.Configuration
{
    /// <summary>
    /// Represents the entirety of options accessors which are used to provide
    /// components of the query execution engine access to settings, which were
    /// provided from the outside, to influence the behaviour of the query
    /// execution engine itself.
    /// </summary>
    public interface IQueryExecutionOptionsAccessor
        : IInstrumentationOptionsAccessor
        , IErrorHandlerOptionsAccessor
        , IQueryCacheSizeOptionsAccessor
        , IRequestTimeoutOptionsAccessor
        , IValidateQueryOptionsAccessor
    { }
}
