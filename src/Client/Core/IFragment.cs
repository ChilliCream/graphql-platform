using System;
using System.Linq.Expressions;

namespace HotChocolate.Client
{
    /// <summary>
    /// Represents a GraphQL fragment.
    /// </summary>
    /// <see cref="Fragment{TValue, TResult}"/>.
    public interface IFragment
    {
        /// <summary>
        /// Gets the name of a fragment.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the selector expression.
        /// </summary>
        Expression Expression { get; }

        /// <summary>
        /// Gets the input type of the fragment.
        /// </summary>
        Type InputType { get; }

        /// <summary>
        /// Gets the output type of the fragment.
        /// </summary>
        Type ReturnType { get; }
    }

    public interface IFragment<TValue, out TResult> : IFragment
    {
    }
}
