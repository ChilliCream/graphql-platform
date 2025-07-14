using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using static HotChocolate.Resolvers.Expressions.Parameters.ParameterExpressionBuilderHelpers;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class SchemaParameterExpressionBuilder
    : IParameterExpressionBuilder
    , IParameterBindingFactory
    , IParameterBinding
{
    private static readonly PropertyInfo s_schema =
        ContextType.GetProperty(nameof(IResolverContext.Schema))!;

    public ArgumentKind Kind => ArgumentKind.Schema;

    public bool IsPure => true;

    public bool IsDefaultHandler => false;

    public bool CanHandle(ParameterInfo parameter)
        => typeof(ISchemaDefinition) == parameter.ParameterType
            || typeof(Schema) == parameter.ParameterType;

    public Expression Build(ParameterExpressionBuilderContext context)
        => Expression.Convert(
            Expression.Property(context.ResolverContext, s_schema),
            context.Parameter.ParameterType);

    public IParameterBinding Create(ParameterBindingContext context)
        => this;

    public T Execute<T>(IResolverContext context)
        => (T)(object)context.Schema;
}
