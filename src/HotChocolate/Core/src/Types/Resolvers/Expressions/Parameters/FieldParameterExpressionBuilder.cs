using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class FieldParameterExpressionBuilder
        : LambdaParameterExpressionBuilder<IPureResolverContext, IOutputField>
    {
        public FieldParameterExpressionBuilder()
            : base(ctx => ctx.Selection.Field)
        {
        }

        public override ArgumentKind Kind => ArgumentKind.Field;

        public override bool CanHandle(ParameterInfo parameter, Type source)
            => typeof(IOutputField).IsAssignableFrom(parameter.ParameterType);

        public override Expression Build(ParameterInfo parameter, Type source, Expression context)
        {
            Expression expression = base.Build(parameter, source, context);

            return parameter.ParameterType != typeof(IOutputField)
                ? Expression.Convert(expression, parameter.ParameterType)
                : expression;
        }
    }
}
