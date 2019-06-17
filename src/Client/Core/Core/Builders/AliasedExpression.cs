using System;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Client.Core.Builders
{
    /// <summary>
    /// Marker to denote that an expression should be assigned an alias in the GraphQL query.
    /// </summary>
    class AliasedExpression : Expression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AliasedExpression"/> class.
        /// </summary>
        /// <param name="inner">The expression.</param>
        /// <param name="alias">The alias.</param>
        public AliasedExpression(Expression inner, MemberInfo alias)
        {
            Inner = inner;
            Alias = alias;
        }

        /// <summary>
        /// Gets the expression that will be assigned an alias.
        /// </summary>
        public Expression Inner { get; }

        /// <summary>
        /// Gets the alias.
        /// </summary>
        public MemberInfo Alias { get; }

        /// <summary>
        /// Wraps an expression in a <see cref="AliasedExpression"/> if <paramref name="alias"/> is
        /// not null.
        /// </summary>
        /// <param name="inner">The expression.</param>
        /// <param name="alias">The alias.</param>
        /// <returns>
        /// An <see cref="AliasedExpression"/> if <paramref name="alias"/> is non-null; otherwise
        /// <paramref name="inner"/>.
        /// </returns>
        public static Expression WrapIfNeeded(Expression inner, MemberInfo alias)
        {
            return alias != null ? new AliasedExpression(inner, alias) : inner;
        }
    }
}
