using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;

namespace HotChocolate.Resolvers.Expressions.Parameters;

/// <summary>
/// Builds parameter expressions for resolver level dependency injection.
/// Parameters need to be annotated with the <see cref="ServiceAttribute"/>.
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

    public bool CanHandle(ParameterDescriptor parameter)
        => parameter.Attributes.Any(t => t is ServiceAttribute);

    public Expression Build(ParameterExpressionBuilderContext context)
    {
        var attribute = context.Parameter.GetCustomAttribute<ServiceAttribute>()!;

        if (attribute.ServiceKey is not null)
        {
            return ServiceExpressionHelper.Build(context.Parameter, context.ResolverContext, attribute.ServiceKey);
        }

        return ServiceExpressionHelper.Build(context.Parameter, context.ResolverContext);
    }

    public IParameterBinding Create(ParameterDescriptor parameter)
        => new ServiceParameterBinding(parameter);

    private sealed class ServiceParameterBinding : IParameterBinding
    {
        public ServiceParameterBinding(ParameterDescriptor parameter)
        {
            ServiceAttribute? service = null;
            foreach (var attribute in parameter.Attributes)
            {
                if (attribute is ServiceAttribute serviceAttribute)
                {
                    service = serviceAttribute;
                    break;
                }
            }

            ServiceKey = service?.ServiceKey;
            IsRequired = !parameter.IsNullable;
        }

        public object? ServiceKey { get; }

        public bool IsRequired { get; }

        public ArgumentKind Kind => ArgumentKind.Service;

        public bool IsPure => true;

#pragma warning disable CS8633
        public T Execute<T>(IResolverContext context) where T : notnull
        {
            if (ServiceKey is not null)
            {
                return IsRequired
                    ? context.Services.GetRequiredKeyedService<T>(ServiceKey)
                    : context.Services.GetKeyedService<T>(ServiceKey)!;
            }

            return IsRequired
                ? context.Services.GetRequiredService<T>()
                : context.Services.GetService<T>()!;
        }
#pragma warning restore CS8633
    }
}
