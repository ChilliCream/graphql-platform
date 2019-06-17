using System;
using System.Linq.Expressions;

namespace HotChocolate.Client
{
    /// <summary>
    /// A GraphQL fragment
    /// </summary>
    /// <typeparam name="TValue">Input type for the fragment</typeparam>
    /// <typeparam name="TResult">The output type of the fragment</typeparam>
    /// <remarks>
    /// Fragments allow you to share field selections between multiple queries and within the same
    /// query: https://graphql.org/learn/queries/#fragments
    /// </remarks>
    public class Fragment<TValue, TResult> : IFragment<TValue, TResult>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Fragment{TValue, TResult}"/> class.
        /// </summary>
        /// <param name="name">The name of the fragment.</param>
        /// <param name="expression">The fragment selector expression.</param>
        public Fragment(string name, Expression<Func<TValue, TResult>> expression)
        {
            Name = name;
            Expression = expression;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public Expression<Func<TValue, TResult>> Expression { get; }

        /// <inheritdoc />
        Type IFragment.InputType => typeof(TValue);

        /// <inheritdoc />
        Type IFragment.ReturnType => typeof(TResult);

        /// <inheritdoc />
        Expression IFragment.Expression => Expression;
    }
}
