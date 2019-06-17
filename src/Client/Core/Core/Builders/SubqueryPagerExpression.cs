using System;
using System.Linq.Expressions;

namespace HotChocolate.Client.Core.Builders
{
    /// <summary>
    /// Marker to denote that a method call is an auto-paging method of a subquery.
    /// </summary>
    class SubqueryPagerExpression : Expression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubqueryPagerExpression"/> class.
        /// </summary>
        /// <param name="methodCall">The method call.</param>
        public SubqueryPagerExpression(MethodCallExpression methodCall)
        {
            MethodCall = methodCall;
        }

        /// <summary>
        /// Gets the method call.
        /// </summary>
        public MethodCallExpression MethodCall { get; }

        /// <inheritdoc/>
        public override ExpressionType NodeType => ExpressionType.Extension;

        /// <inheritdoc/>
        public override Type Type => MethodCall.Type;
    }
}
