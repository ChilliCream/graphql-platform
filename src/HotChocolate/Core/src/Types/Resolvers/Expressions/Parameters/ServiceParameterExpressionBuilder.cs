using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

/// <summary>
/// Builds parameter expressions for resolver level dependency injection.
/// Parameters need to be annotated with the <see cref="ServiceAttribute"/> or the
/// <c>FromServicesAttribute</c>.
/// </summary>
internal sealed class ServiceParameterExpressionBuilder
    : IParameterExpressionBuilder
    , IParameterBindingFactory
{
    public ArgumentKind Kind => ArgumentKind.Service;

    public bool IsPure => true;

    public bool IsDefaultHandler => false;

    public bool CanHandle(ParameterInfo parameter)
        => parameter.IsDefined(typeof(ServiceAttribute), false);

    public Expression Build(ParameterExpressionBuilderContext context)
    {
        var attribute = context.Parameter.GetCustomAttribute<ServiceAttribute>()!;

        if (!string.IsNullOrEmpty(attribute.Key))
        {
            return ServiceExpressionHelper.Build(context.Parameter, context.ResolverContext, attribute.Key);
        }

        return ServiceExpressionHelper.Build(context.Parameter, context.ResolverContext);
    }

    public IParameterBinding Create(ParameterBindingContext context)
        => new ServiceParameterBinding(context.Parameter);

    private sealed class ServiceParameterBinding : IParameterBinding
    {
        public ServiceParameterBinding(ParameterInfo parameter)
        {
            var attribute = parameter.GetCustomAttribute<ServiceAttribute>();
            Key = attribute?.Key;
        }

        public string? Key { get; }

        public ArgumentKind Kind => ArgumentKind.Service;

        public bool IsPure => true;

        public T Execute<T>(IResolverContext context) where T : notnull
        {
            if (Key is not null)
            {
                return context.Service<T>(Key)!;
            }

            return context.Service<T>();
        }
    }
}
