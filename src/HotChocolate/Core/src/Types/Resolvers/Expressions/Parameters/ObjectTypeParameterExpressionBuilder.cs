using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class ObjectTypeParameterExpressionBuilder
        : LambdaParameterExpressionBuilder<IResolverContext, IObjectType>
    {
        public ObjectTypeParameterExpressionBuilder()
            : base(ctx => ctx.ObjectType)
        {
        }

        public override ArgumentKind Kind => ArgumentKind.ObjectType;

        public override bool CanHandle(ParameterInfo parameter, Type source)
            => typeof(ObjectType) == parameter.ParameterType ||
               typeof(IObjectType) == parameter.ParameterType;

        public override Expression Build(ParameterInfo parameter, Type source, Expression context)
        {
            Expression expression = base.Build(parameter, source, context);

            return parameter.ParameterType == typeof(ObjectType)
                ? Expression.Convert(expression, typeof(ObjectType))
                : expression;
        }
    }
}
