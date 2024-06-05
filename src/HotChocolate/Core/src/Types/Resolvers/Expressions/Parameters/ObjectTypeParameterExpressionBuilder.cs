using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class ObjectTypeParameterExpressionBuilder
    : LambdaParameterExpressionBuilder<IPureResolverContext, IObjectType>
    , IParameterBindingFactory
    , IParameterBinding
{
    public ObjectTypeParameterExpressionBuilder()
        : base(ctx => ctx.ObjectType)
    {
    }

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
        => (T)(object)context.ObjectType;

    public T Execute<T>(IPureResolverContext context)
        => (T)(object)context.ObjectType;
}
