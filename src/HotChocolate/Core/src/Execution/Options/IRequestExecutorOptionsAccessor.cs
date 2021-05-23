﻿namespace HotChocolate.Execution.Options
{
    /// <summary>
    /// Represents the entirety of options accessors which are used to provide
    /// components of the query execution engine access to settings, which were
    /// provided from the outside, to influence the behavior of the query
    /// execution engine itself.
    /// </summary>
    public interface IRequestExecutorOptionsAccessor
        : IInstrumentationOptionsAccessor
        , IErrorHandlerOptionsAccessor
        , IDocumentCacheSizeOptionsAccessor
        , IRequestTimeoutOptionsAccessor
        , IComplexityAnalyzerOptionsAccessor
    { }
}
