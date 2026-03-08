using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Internal;
using HotChocolate.Types;

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class ObjectTypeParameterExpressionBuilder()
    : LambdaParameterExpressionBuilder<IObjectTypeDefinition>(ctx => ctx.ObjectType, isPure: true)
    , IParameterBindingFactory
    , IParameterBinding
{
    public override ArgumentKind Kind => ArgumentKind.ObjectType;

    public override bool CanHandle(ParameterInfo parameter)
        => typeof(ObjectType) == parameter.ParameterType
            || typeof(IObjectTypeDefinition) == parameter.ParameterType;

    public bool CanHandle(ParameterDescriptor parameter)
        => typeof(ObjectType) == parameter.Type
            || typeof(IObjectTypeDefinition) == parameter.Type;

    public override Expression Build(ParameterExpressionBuilderContext context)
    {
        var expression = base.Build(context);

        return context.Parameter.ParameterType == typeof(ObjectType)
            ? Expression.Convert(expression, typeof(ObjectType))
            : expression;
    }

    public IParameterBinding Create(ParameterDescriptor parameter)
        => this;

    public T Execute<T>(IResolverContext context)
    {
        Debug.Assert(typeof(T) == typeof(ObjectType) || typeof(T) == typeof(IObjectTypeDefinition));
        var objectType = context.ObjectType;
        return Unsafe.As<ObjectType, T>(ref objectType);
    }
}
