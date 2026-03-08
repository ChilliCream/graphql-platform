using HotChocolate.Internal;
using HotChocolate.Resolvers.Expressions.Parameters;

namespace HotChocolate.Resolvers;

public sealed class ParameterBindingResolver
{
    private readonly IParameterBindingFactory[] _bindings;
    private readonly IParameterBindingFactory _defaultBinding;

    public ParameterBindingResolver(
        IServiceProvider applicationServices,
        IEnumerable<IParameterExpressionBuilder>? customBindingFactories)
    {
        var serviceInspector = applicationServices.GetService<IServiceProviderIsService>();
        var factories = customBindingFactories?.OfType<IParameterBindingFactory>().ToArray() ?? [];

        // explicit internal expression builders will be added first.
        var bindingFactories = new List<IParameterBindingFactory>
        {
            new ParentParameterExpressionBuilder(),
            new ServiceParameterExpressionBuilder(),
            new ArgumentParameterExpressionBuilder(),
            new GlobalStateParameterExpressionBuilder(),
            new ScopedStateParameterExpressionBuilder(),
            new LocalStateParameterExpressionBuilder(),
            new IsSelectedParameterExpressionBuilder(),
            new EventMessageParameterExpressionBuilder()
        };

        if (customBindingFactories is not null)
        {
            // then we will add custom parameter expression builder and
            // give the user a chance to override our implicit expression builder.
            bindingFactories.AddRange(factories.Where(t => !t.IsDefaultHandler));
        }

        if (serviceInspector is not null)
        {
            bindingFactories.Add(new InferredServiceParameterExpressionBuilder(serviceInspector));
        }

        bindingFactories.Add(new DocumentParameterExpressionBuilder());
        bindingFactories.Add(new CancellationTokenParameterExpressionBuilder());
        bindingFactories.Add(new ResolverContextParameterExpressionBuilder());
        bindingFactories.Add(new SchemaParameterExpressionBuilder());
        bindingFactories.Add(new SelectionParameterExpressionBuilder());
        bindingFactories.Add(new ObjectTypeParameterExpressionBuilder());
        bindingFactories.Add(new OperationDefinitionParameterExpressionBuilder());
        bindingFactories.Add(new OperationParameterExpressionBuilder());
        bindingFactories.Add(new FieldParameterExpressionBuilder());
        bindingFactories.Add(new ClaimsPrincipalParameterExpressionBuilder());
        bindingFactories.Add(new PathParameterExpressionBuilder());
        bindingFactories.Add(new ConnectionFlagsParameterExpressionBuilder());

        if (customBindingFactories is not null)
        {
            bindingFactories.AddRange(factories.Where(t => t.IsDefaultHandler));
        }

        _bindings = [.. bindingFactories];
        _defaultBinding = new ArgumentParameterExpressionBuilder();
    }

    public IParameterBinding GetBinding(ParameterDescriptor parameter)
    {
        foreach (var binding in _bindings)
        {
            if (binding.CanHandle(parameter))
            {
                return binding.Create(parameter);
            }
        }

        return _defaultBinding.Create(parameter);
    }

    public (ArgumentKind Kind, bool IsPure) GetBindingInfo(ParameterDescriptor parameter)
    {
        foreach (var binding in _bindings)
        {
            if (binding.CanHandle(parameter))
            {
                return (binding.Kind, binding.IsPure);
            }
        }

        return (_defaultBinding.Kind, _defaultBinding.IsPure);
    }
}
