using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class FieldParameterExpressionBuilder
    : LambdaParameterExpressionBuilder<IPureResolverContext, IObjectField>
{
    public FieldParameterExpressionBuilder()
        : base(ctx => ctx.Selection.Field)
    {
    }

    public override ArgumentKind Kind => ArgumentKind.Field;

    public override bool CanHandle(ParameterInfo parameter)
        => typeof(IOutputField).IsAssignableFrom(parameter.ParameterType);

    public override Expression Build(ParameterInfo parameter, Expression context)
    {
        var expression = base.Build(parameter, context);

        return parameter.ParameterType != typeof(IOutputField)
            ? Expression.Convert(expression, parameter.ParameterType)
            : expression;
    }
}
