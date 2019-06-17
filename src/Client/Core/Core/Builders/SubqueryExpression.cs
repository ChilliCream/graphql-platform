using System;
using System.Linq.Expressions;

namespace HotChocolate.Client.Core.Builders
{
    /// <summary>
    /// Marker to associate a method call with a subquery.
    /// </summary>
    class SubqueryExpression : Expression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubqueryExpression"/> class.
        /// </summary>
        /// <param name="subquery">The subquery.</param>
        /// <param name="methodCall">The method call.</param>
        public SubqueryExpression(ISubquery subquery, MethodCallExpression methodCall)
        {
            Subquery = subquery;
            MethodCall = methodCall;
        }

        /// <summary>
        /// Gets the method call.
        /// </summary>
        public MethodCallExpression MethodCall { get; }

        /// <summary>
        /// Gets the associated subquery.
        /// </summary>
        public ISubquery Subquery { get; }

        /// <inheritdoc/>
        public override ExpressionType NodeType => ExpressionType.Extension;

        /// <inheritdoc/>
        public override Type Type => MethodCall.Type;
    }
}
