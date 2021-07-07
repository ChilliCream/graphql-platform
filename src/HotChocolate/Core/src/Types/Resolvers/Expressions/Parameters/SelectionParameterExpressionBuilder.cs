using System;
using System.Linq.Expressions;
using System.Reflection;

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

        public override bool CanHandle(ParameterInfo parameter, Type source)
            => typeof(IFieldSelection).IsAssignableFrom(parameter.ParameterType);

        public override Expression Build(ParameterInfo parameter, Type source, Expression context)
            => Expression.Convert(base.Build(parameter, source, context), parameter.ParameterType);
    }
}
