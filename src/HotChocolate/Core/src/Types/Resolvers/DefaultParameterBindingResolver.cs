#nullable enable

using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Resolvers.Expressions.Parameters;

namespace HotChocolate.Resolvers;

public sealed class DefaultParameterBindingResolver : IParameterBindingResolver
{
    private readonly IParameterBindingFactory[] _bindings;
    private readonly IParameterBindingFactory _defaultBinding;

    public DefaultParameterBindingResolver(
        IServiceProvider applicationServices,
        IEnumerable<IParameterExpressionBuilder>? customBindingFactories)
    {
        var serviceInspector = applicationServices.GetService<IServiceProviderIsService>();

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
            bindingFactories.AddRange(
                customBindingFactories
                    .Where(t => !t.IsDefaultHandler)
                    .OfType<IParameterBindingFactory>());
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
        bindingFactories.Add(new FieldSyntaxParameterExpressionBuilder());
        bindingFactories.Add(new ObjectTypeParameterExpressionBuilder());
        bindingFactories.Add(new OperationDefinitionParameterExpressionBuilder());
        bindingFactories.Add(new OperationParameterExpressionBuilder());
        bindingFactories.Add(new FieldParameterExpressionBuilder());
        bindingFactories.Add(new ClaimsPrincipalParameterExpressionBuilder());
        bindingFactories.Add(new PathParameterExpressionBuilder());

         if (customBindingFactories is not null)
        {
            bindingFactories.AddRange(
                customBindingFactories
                    .Where(t => t.IsDefaultHandler)
                    .OfType<IParameterBindingFactory>());
        }

        _bindings = [.. bindingFactories];
        _defaultBinding = new ArgumentParameterExpressionBuilder();
    }

    public IParameterBinding GetBinding(ParameterInfo parameter)
    {
        var context = new ParameterBindingContext(parameter, parameter.Name);

        foreach (var binding in _bindings)
        {
            if (binding.CanHandle(parameter))
            {
                return binding.Create(context);
            }
        }

        return _defaultBinding.Create(context);
    }
}
