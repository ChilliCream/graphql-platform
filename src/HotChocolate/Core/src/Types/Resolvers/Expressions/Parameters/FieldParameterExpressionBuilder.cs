using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class FieldParameterExpressionBuilder()
    : LambdaParameterExpressionBuilder<IObjectField>(ctx => ctx.Selection.Field, isPure: true)
    , IParameterBindingFactory
    , IParameterBinding
{
    public override ArgumentKind Kind => ArgumentKind.Field;

    public override bool CanHandle(ParameterInfo parameter)
        => typeof(IOutputField).IsAssignableFrom(parameter.ParameterType);

    public override Expression Build(ParameterExpressionBuilderContext context)
    {
        var expression = base.Build(context);
        var parameter = context.Parameter;

        return parameter.ParameterType != typeof(IOutputField)
            ? Expression.Convert(expression, parameter.ParameterType)
            : expression;
    }

    public IParameterBinding Create(ParameterBindingContext context)
        => this;

    public T Execute<T>(IResolverContext context)
        => (T)context.Selection.Field;
}
