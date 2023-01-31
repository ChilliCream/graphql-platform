using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

/// <summary>
/// Builds parameter expressions for resolver level dependency injection.
/// Parameters need to be annotated with the <see cref="ServiceAttribute"/> or the
/// <c>FromServicesAttribute</c>.
/// </summary>
internal sealed class ServiceParameterExpressionBuilder
    : IParameterExpressionBuilder
    , IParameterFieldConfiguration
{
    public ArgumentKind Kind => ArgumentKind.Service;

    public bool IsPure => true;

    public bool IsDefaultHandler => false;

    public bool CanHandle(ParameterInfo parameter)
        => ServiceExpressionHelper.TryGetServiceKind(parameter, out var kind) &&
           kind is ServiceKind.Default;

    public void ApplyConfiguration(ParameterInfo parameter, ObjectFieldDescriptor descriptor)
        => ServiceExpressionHelper.ApplyConfiguration(parameter, descriptor, ServiceKind.Default);

    public Expression Build(ParameterExpressionBuilderContext context)
        => ServiceExpressionHelper.Build(
            context.Parameter,
            context.ResolverContext,
            ServiceKind.Default);
}
