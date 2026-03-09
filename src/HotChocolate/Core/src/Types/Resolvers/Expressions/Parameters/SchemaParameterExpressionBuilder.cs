using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Internal;
using static HotChocolate.Resolvers.Expressions.Parameters.ParameterExpressionBuilderHelpers;

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

    public bool CanHandle(ParameterDescriptor parameter)
        => typeof(ISchemaDefinition) == parameter.Type
            || typeof(Schema) == parameter.Type;

    public Expression Build(ParameterExpressionBuilderContext context)
        => Expression.Convert(
            Expression.Property(context.ResolverContext, s_schema),
            context.Parameter.ParameterType);

    public IParameterBinding Create(ParameterDescriptor parameter)
        => this;

    public T Execute<T>(IResolverContext context)
    {
        Debug.Assert(typeof(T) == typeof(Schema) || typeof(T) == typeof(ISchemaDefinition));
        var schema = context.Schema;
        return Unsafe.As<Schema, T>(ref schema);
    }
}
