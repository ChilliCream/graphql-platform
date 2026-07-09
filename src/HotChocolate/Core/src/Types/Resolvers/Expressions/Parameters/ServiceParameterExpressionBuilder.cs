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

        if (!string.IsNullOrEmpty(attribute.Key))
        {
            return ServiceExpressionHelper.Build(context.Parameter, context.ResolverContext, attribute.Key);
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

            Key = service?.Key;
            IsRequired = !parameter.IsNullable;
        }

        public string? Key { get; }

        public bool IsRequired { get; }

        public ArgumentKind Kind => ArgumentKind.Service;

        public bool IsPure => true;

#pragma warning disable CS8633
        public T Execute<T>(IResolverContext context) where T : notnull
        {
            if (Key is not null)
            {
                return IsRequired
                    ? context.Services.GetRequiredKeyedService<T>(Key)
                    : context.Services.GetKeyedService<T>(Key)!;
            }

            return IsRequired
                ? context.Services.GetRequiredService<T>()
                : context.Services.GetService<T>()!;
        }
#pragma warning restore CS8633
    }
}
