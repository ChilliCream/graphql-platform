using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class FieldParameterExpressionBuilder()
    : LambdaParameterExpressionBuilder<IOutputFieldDefinition>(ctx => ctx.Selection.Field, isPure: true)
    , IParameterBindingFactory
    , IParameterBinding
{
    public override ArgumentKind Kind => ArgumentKind.Field;

    public override bool CanHandle(ParameterInfo parameter)
        => typeof(IOutputFieldDefinition).IsAssignableFrom(parameter.ParameterType);

    public bool CanHandle(ParameterDescriptor parameter)
        => typeof(IOutputFieldDefinition).IsAssignableFrom(parameter.Type);

    public override Expression Build(ParameterExpressionBuilderContext context)
    {
        var expression = base.Build(context);
        var parameter = context.Parameter;

        return parameter.ParameterType != typeof(IOutputFieldDefinition)
            ? Expression.Convert(expression, parameter.ParameterType)
            : expression;
    }

    public IParameterBinding Create(ParameterDescriptor parameter)
        => this;

    public T Execute<T>(IResolverContext context)
    {
        Debug.Assert(typeof(T) == typeof(ObjectField) || typeof(T) == typeof(IOutputFieldDefinition));
        var field = context.Selection.Field;
        return Unsafe.As<ObjectField, T>(ref field);
    }
}
