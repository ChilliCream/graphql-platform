using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;

namespace HotChocolate.Resolvers.Expressions.Parameters;

/// <summary>
/// Builds parameter expressions for resolver level dependency injection for inferred services.
/// </summary>
internal sealed class InferredServiceParameterExpressionBuilder(IServiceProviderIsService serviceInspector)
    : IParameterExpressionBuilder
    , IParameterBindingFactory
    , IParameterBinding
{
    public ArgumentKind Kind => ArgumentKind.Service;

    public bool IsPure => true;

    public bool IsDefaultHandler => false;

    public bool CanHandle(ParameterInfo parameter)
    {
        if (parameter.ParameterType.IsGenericType
            && typeof(IEnumerable).IsAssignableFrom(parameter.ParameterType)
            && parameter.ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return serviceInspector.IsService(parameter.ParameterType.GetGenericArguments()[0]);
        }

        return serviceInspector.IsService(parameter.ParameterType);
    }

    public bool CanHandle(ParameterDescriptor parameter)
    {
        if (parameter.Type.IsGenericType
            && typeof(IEnumerable).IsAssignableFrom(parameter.Type)
            && parameter.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return serviceInspector.IsService(parameter.Type.GetGenericArguments()[0]);
        }

        return serviceInspector.IsService(parameter.Type);
    }

    public Expression Build(ParameterExpressionBuilderContext context)
        => ServiceExpressionHelper.Build(context.Parameter, context.ResolverContext);

    public IParameterBinding Create(ParameterDescriptor parameter)
        => this;

#pragma warning disable CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.
    public T Execute<T>(IResolverContext context) where T : notnull
        => context.Services.GetRequiredService<T>();
#pragma warning restore CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.
}
