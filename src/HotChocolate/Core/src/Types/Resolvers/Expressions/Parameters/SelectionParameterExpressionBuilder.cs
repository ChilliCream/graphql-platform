using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class SelectionParameterExpressionBuilder
        : LambdaParameterExpressionBuilder<IPureResolverContext, object>
    {
        public SelectionParameterExpressionBuilder()
            : base(ctx => ctx.Selection)
        {
        }

        public override ArgumentKind Kind => ArgumentKind.Selection;

        public override bool CanHandle(ParameterInfo parameter)
            => typeof(IFieldSelection).IsAssignableFrom(parameter.ParameterType);

        public override Expression Build(ParameterInfo parameter, Expression context)
            => Expression.Convert(base.Build(parameter, context), parameter.ParameterType);
    }
}
