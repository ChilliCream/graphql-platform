using System;
using static HotChocolate.Properties.AbstractionResources;

namespace HotChocolate.Execution;

/// <summary>
/// Helper methods for <see cref="IExecutionResult"/>.
/// </summary>
public static class ExecutionResultExtensions
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static IQueryResult ExpectQueryResult(this IExecutionResult result)
    {
        if (result is IQueryResult qr)
        {
            return qr;
        }

        throw new ArgumentException(
            ExecutionResultExtensions_ExpectQueryResult_NotQueryResult);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static IResponseStream ExpectResponseStream(this IExecutionResult result)
    {
        if (result is IResponseStream rs)
        {
            return rs;
        }

        throw new ArgumentException(
            ExecutionResultExtensions_ExpectResponseStream_NotResponseStream);
    }
}
