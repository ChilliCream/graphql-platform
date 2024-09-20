using HotChocolate.Data.Raven;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;

namespace HotChocolate.Data;

/// <summary>
/// Common extension for <see cref="IAsyncDocumentQuery{T}"/> for executable
/// </summary>
public static class RavenExecutableExtensions
{
    /// <summary>
    /// Wraps a <see cref="IAsyncDocumentQuery{TDocument}"/> with
    /// <see cref="RavenAsyncDocumentQueryExecutable{T}"/> to help the execution engine to execute it
    /// more efficiently
    /// </summary>
    /// <param name="collection">The source of the <see cref="IExecutable"/></param>
    /// <typeparam name="T">The type parameter</typeparam>
    /// <returns>The wrapped object</returns>
    public static IExecutable<T> AsExecutable<T>(this IAsyncDocumentQuery<T> collection)
    {
        return new RavenAsyncDocumentQueryExecutable<T>(collection);
    }

    /// <summary>
    /// Wraps a <see cref="IRavenQueryable{TDocument}"/> with
    /// <see cref="RavenAsyncDocumentQueryExecutable{T}"/> to help the execution engine to execute it
    /// more efficiently
    /// </summary>
    /// <param name="collection">The source of the <see cref="IExecutable"/></param>
    /// <typeparam name="T">The type parameter</typeparam>
    /// <returns>The wrapped object</returns>
    public static IExecutable<T> AsExecutable<T>(this IRavenQueryable<T> collection)
    {
        return new RavenAsyncDocumentQueryExecutable<T>(collection.ToAsyncDocumentQuery());
    }
}
