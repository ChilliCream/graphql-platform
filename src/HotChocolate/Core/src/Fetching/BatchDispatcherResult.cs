using System;
using System.Collections.Generic;
using HotChocolate.Fetching.Properties;

namespace HotChocolate.Fetching;

/// <summary>
/// Represents the outcome of dispatching enqueued batches.
/// </summary>
public class BatchDispatcherResult
{
    private BatchDispatcherResult()
    {
        IsSuccessful = true;
        Exceptions = Array.Empty<Exception>();
    }

    /// <summary>
    /// Creates a new error result.
    /// </summary>
    /// <param name="exceptions">
    /// The exceptions that occured while dispatching enqueued batches.
    /// </param>
    public BatchDispatcherResult(IReadOnlyList<Exception> exceptions)
    {
        if (exceptions is null)
        {
            throw new ArgumentNullException(nameof(exceptions));
        }

        if (exceptions.Count == 0)
        {
            throw new ArgumentException(
                FetchingResources.BatchDispatcherResult_NoExceptions,
                nameof(exceptions));
        }

        IsSuccessful = false;
        Exceptions = exceptions;
    }

    /// <summary>
    /// Specifies that the execution of the enqueued batches was successful.
    /// </summary>
    public bool IsSuccessful { get; }

    /// <summary>
    /// Gets the list of exceptions that occured during the execution of the enqueued
    /// batches if <see cref="IsSuccessful"/> is <c>false</c>.
    /// </summary>
    public IReadOnlyList<Exception> Exceptions { get; }

    /// <summary>
    /// Gets the cached success result.
    /// </summary>
    public static BatchDispatcherResult Success { get; } = new();
}
