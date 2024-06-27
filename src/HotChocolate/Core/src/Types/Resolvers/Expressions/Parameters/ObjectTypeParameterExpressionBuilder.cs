using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class ObjectTypeParameterExpressionBuilder()
    : LambdaParameterExpressionBuilder<IObjectType>(ctx => ctx.ObjectType, isPure: true)
    , IParameterBindingFactory
    , IParameterBinding
{
    public override ArgumentKind Kind => ArgumentKind.ObjectType;

    public override bool CanHandle(ParameterInfo parameter)
        => typeof(ObjectType) == parameter.ParameterType ||
           typeof(IObjectType) == parameter.ParameterType;

    public override Expression Build(ParameterExpressionBuilderContext context)
    {
        var expression = base.Build(context);

        return context.Parameter.ParameterType == typeof(ObjectType)
            ? Expression.Convert(expression, typeof(ObjectType))
            : expression;
    }

    public IParameterBinding Create(ParameterBindingContext context)
        => this;

    public T Execute<T>(IResolverContext context)
        => (T)context.ObjectType;
}
