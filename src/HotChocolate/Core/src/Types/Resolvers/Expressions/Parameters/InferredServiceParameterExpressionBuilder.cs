#nullable enable

using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;

namespace HotChocolate.Resolvers.Expressions.Parameters;

/// <summary>
/// Builds parameter expressions for resolver level dependency injection for inferred services.
/// </summary>
internal sealed class InferredServiceParameterExpressionBuilder(IServiceProviderIsService serviceInspector)
    : IParameterExpressionBuilder
{
    public ArgumentKind Kind => ArgumentKind.Service;

    public bool IsPure => true;

    public bool IsDefaultHandler => false;

    public bool CanHandle(ParameterInfo parameter)
        => serviceInspector.IsService(parameter.ParameterType);

    public Expression Build(ParameterExpressionBuilderContext context)
    {
#if NET8_0_OR_GREATER
        return ServiceExpressionHelper.TryGetServiceKey(context.Parameter, out var key)
            ? ServiceExpressionHelper.Build(
                context.Parameter,
                context.ResolverContext,
                ServiceKind.Default,
                key)
            : ServiceExpressionHelper.Build(
                context.Parameter,
                context.ResolverContext,
                ServiceKind.Default);
#else
        return ServiceExpressionHelper.Build(
            context.Parameter,
            context.ResolverContext,
            ServiceKind.Default);
#endif
    }
}