using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class ObjectTypeParameterExpressionBuilder
    : LambdaParameterExpressionBuilder<IPureResolverContext, IObjectType>
{
    public ObjectTypeParameterExpressionBuilder()
        : base(ctx => ctx.ObjectType)
    {
    }

    public override ArgumentKind Kind => ArgumentKind.ObjectType;

    public override bool CanHandle(ParameterInfo parameter)
        => typeof(ObjectType) == parameter.ParameterType ||
           typeof(IObjectType) == parameter.ParameterType;

    public override Expression Build(ParameterInfo parameter, Expression context)
    {
        var expression = base.Build(parameter, context);

        return parameter.ParameterType == typeof(ObjectType)
            ? Expression.Convert(expression, typeof(ObjectType))
            : expression;
    }
}
