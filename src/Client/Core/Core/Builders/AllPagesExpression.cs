using System;
using System.Linq.Expressions;

namespace HotChocolate.Client.Core.Builders
{
    /// <summary>
    /// Marker to denote that an AllPages() call was made on a method.
    /// </summary>
    class AllPagesExpression : Expression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AllPagesExpression"/> class.
        /// </summary>
        /// <param name="method">The method that AllPages() was called on.</param>
        /// <param name="pageSize">The ConstantExpression that AllPages was sent</param>
        public AllPagesExpression(MethodCallExpression method, int? pageSize = null)
        {
            Method = method;
            PageSize = pageSize;
        }

        /// <summary>
        /// Gets the method that AllPages() was called on.
        /// </summary>
        public MethodCallExpression Method { get; }

        /// <summary>
        /// Gets the value that was sent to AllPages
        /// </summary>
        public int? PageSize { get; }

        /// <inheritdoc/>
        public override ExpressionType NodeType => ExpressionType.Extension;

        /// <inheritdoc/>
        public override Type Type => Method.Type;
    }
}
