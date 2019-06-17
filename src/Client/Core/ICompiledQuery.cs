using System;
using System.Collections.Generic;
using HotChocolate.Client.Core;

namespace HotChocolate.Client
{
    /// <summary>
    /// Represents a compiled GraphQL query.
    /// </summary>
    public interface ICompiledQuery
    {
        /// <summary>
        /// Gets a value indicating whether the query is a mutation.
        /// </summary>
        bool IsMutation { get; }

        /// <summary>
        /// Returns an <see cref="IQueryRunner"/> which can be used to run the query on a connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="variables">The query variables.</param>
        /// <returns>A query runner.</returns>
        IQueryRunner Start(IConnection connection, IDictionary<string, object> variables);

        /// <summary>
        /// Returns a string representation of the query with the specified indentation.
        /// </summary>
        /// <param name="indentation">The indentation.</param>
        string ToString(int indentation);
    }

    /// <summary>
    /// Represents a compiled GraphQL query.
    /// </summary>
    /// <typeparam name="TResult">The query result type.</typeparam>
    public interface ICompiledQuery<TResult> : ICompiledQuery
    {
        /// <summary>
        /// Returns an <see cref="IQueryRunner{TResult}"/> which can be used to run the query on a connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="variables">The query variables.</param>
        /// <returns>A query runner.</returns>
        new IQueryRunner<TResult> Start(IConnection connection, IDictionary<string, object> variables);
    }
}
