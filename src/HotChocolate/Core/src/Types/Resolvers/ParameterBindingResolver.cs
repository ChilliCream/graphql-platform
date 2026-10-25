using HotChocolate.Internal;
using HotChocolate.Resolvers.Expressions.Parameters;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Resolvers;

public sealed class ParameterBindingResolver
{
    private readonly IParameterBindingFactory[] _bindings;
    private readonly IParameterBindingFactory _defaultBinding;
    private readonly IParameterDescriptorFieldConfiguration[] _fieldConfigurations;

    public ParameterBindingResolver(
        IServiceProvider applicationServices,
        IEnumerable<IParameterExpressionBuilder>? customBindingFactories)
    {
        var serviceInspector = applicationServices.GetService<IServiceProviderIsService>();
        var custom = customBindingFactories?.ToArray() ?? [];

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
        var fieldConfigurations = bindingFactories
            .OfType<IParameterDescriptorFieldConfiguration>()
            .ToList();

        if (custom.Length > 0)
        {
            // then we will add custom parameter expression builder and
            // give the user a chance to override our implicit expression builder.
            foreach (var builder in custom)
            {
                if (!builder.IsDefaultHandler)
                {
                    if (builder is IParameterBindingFactory bindingFactory)
                    {
                        bindingFactories.Add(bindingFactory);
                    }

                    if (builder is IParameterDescriptorFieldConfiguration configuration)
                    {
                        fieldConfigurations.Add(configuration);
                    }
                }
            }
        }

        if (serviceInspector is not null)
        {
            AddBindingFactory(
                bindingFactories,
                fieldConfigurations,
                new InferredServiceParameterExpressionBuilder(serviceInspector));
        }

        AddBindingFactory(
            bindingFactories,
            fieldConfigurations,
            new DocumentParameterExpressionBuilder());
        AddBindingFactory(
            bindingFactories,
            fieldConfigurations,
            new CancellationTokenParameterExpressionBuilder());
        AddBindingFactory(
            bindingFactories,
            fieldConfigurations,
            new ResolverContextParameterExpressionBuilder());
        AddBindingFactory(
            bindingFactories,
            fieldConfigurations,
            new SchemaParameterExpressionBuilder());
        AddBindingFactory(
            bindingFactories,
            fieldConfigurations,
            new SelectionParameterExpressionBuilder());
        AddBindingFactory(
            bindingFactories,
            fieldConfigurations,
            new ObjectTypeParameterExpressionBuilder());
        AddBindingFactory(
            bindingFactories,
            fieldConfigurations,
            new OperationDefinitionParameterExpressionBuilder());
        AddBindingFactory(
            bindingFactories,
            fieldConfigurations,
            new OperationParameterExpressionBuilder());
        AddBindingFactory(
            bindingFactories,
            fieldConfigurations,
            new FieldParameterExpressionBuilder());
        AddBindingFactory(
            bindingFactories,
            fieldConfigurations,
            new ClaimsPrincipalParameterExpressionBuilder());
        AddBindingFactory(
            bindingFactories,
            fieldConfigurations,
            new PathParameterExpressionBuilder());
        AddBindingFactory(
            bindingFactories,
            fieldConfigurations,
            new ConnectionFlagsParameterExpressionBuilder());

        if (custom.Length > 0)
        {
            foreach (var builder in custom)
            {
                if (builder.IsDefaultHandler)
                {
                    if (builder is IParameterBindingFactory bindingFactory)
                    {
                        bindingFactories.Add(bindingFactory);
                    }

                    if (builder is IParameterDescriptorFieldConfiguration configuration)
                    {
                        fieldConfigurations.Add(configuration);
                    }
                }
            }
        }

        _bindings = [.. bindingFactories];
        _defaultBinding = new ArgumentParameterExpressionBuilder();
        _fieldConfigurations = [.. fieldConfigurations];
    }

    public IParameterBinding GetBinding(ParameterDescriptor parameter)
        => GetBinding(parameter, out _);

    public IParameterBinding GetBinding(
        ParameterDescriptor parameter,
        out ArgumentKind kind)
    {
        foreach (var binding in _bindings)
        {
            EnsureParameterInfoNotRequired(binding, parameter);

            if (binding.CanHandle(parameter))
            {
                kind = binding.Kind;
                return binding.Create(parameter);
            }
        }

        kind = _defaultBinding.Kind;
        return _defaultBinding.Create(parameter);
    }

    public (ArgumentKind Kind, bool IsPure) GetBindingInfo(ParameterDescriptor parameter)
    {
        foreach (var binding in _bindings)
        {
            EnsureParameterInfoNotRequired(binding, parameter);

            if (binding.CanHandle(parameter))
            {
                return (binding.Kind, binding.IsPure);
            }
        }

        return (_defaultBinding.Kind, _defaultBinding.IsPure);
    }

    public void ApplyConfiguration(
        ParameterDescriptor parameter,
        ObjectFieldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        foreach (var configuration in _fieldConfigurations)
        {
            if (configuration.CanHandle(parameter))
            {
                configuration.ApplyConfiguration(parameter, descriptor);
                break;
            }
        }
    }

    private static void AddBindingFactory(
        List<IParameterBindingFactory> bindingFactories,
        List<IParameterDescriptorFieldConfiguration> fieldConfigurations,
        IParameterBindingFactory bindingFactory)
    {
        bindingFactories.Add(bindingFactory);

        if (bindingFactory is IParameterDescriptorFieldConfiguration configuration)
        {
            fieldConfigurations.Add(configuration);
        }
    }

    private static void EnsureParameterInfoNotRequired(
        IParameterBindingFactory binding,
        ParameterDescriptor parameter)
    {
        if (binding is CustomParameterExpressionBuilder customBuilder
            && customBuilder.RequiresParameterInfo(parameter))
        {
            throw new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "Custom parameter expression builders that use a ParameterInfo predicate "
                        + "cannot be used with source-generated resolvers. Omit the canHandle "
                        + "predicate to match parameters by type.")
                    .Build());
        }
    }
}
