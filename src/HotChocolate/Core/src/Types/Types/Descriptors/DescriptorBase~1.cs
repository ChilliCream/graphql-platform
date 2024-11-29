using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Descriptors;

public abstract class DescriptorBase<T>(IDescriptorContext context)
    : IDescriptor<T>
    , IDescriptorExtension<T>
    , IDescriptorExtension
    , IDefinitionFactory<T>
    where T : DefinitionBase
{
    protected internal IDescriptorContext Context { get; } =
        context ?? throw new ArgumentNullException(nameof(context));

    IDescriptorContext IHasDescriptorContext.Context => Context;

    protected internal abstract T Definition { get; protected set; }

    T IDescriptorExtension<T>.Definition => Definition;

    public IDescriptorExtension<T> Extend() => this;

    public IDescriptorExtension<T> ExtendWith(
        Action<IDescriptorExtension<T>> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        configure(this);
        return this;
    }

    public T CreateDefinition()
    {
        OnCreateDefinition(Definition);

        if (Definition.HasConfigurations)
        {
            var i = 0;
            var configurations = Definition.Configurations;

            do
            {
                if (configurations[i] is { On: ApplyConfigurationOn.Create, } config)
                {
                    configurations.RemoveAt(i);
                    ((CreateConfiguration)config).Configure(Context);
                }
                else
                {
                    i++;
                }
            } while (i < configurations.Count);
        }

        return Definition;
    }

    protected virtual void OnCreateDefinition(T definition)
    {
    }

    DefinitionBase IDefinitionFactory.CreateDefinition()
        => CreateDefinition();

    void IDescriptorExtension<T>.OnBeforeCreate(
        Action<T> configure)
        => OnBeforeCreate((_, d) => configure(d));

    void IDescriptorExtension<T>.OnBeforeCreate(
        Action<IDescriptorContext, T> configure)
        => OnBeforeCreate(configure);

    void IDescriptorExtension.OnBeforeCreate(
        Action<DefinitionBase> configure)
        => OnBeforeCreate((_, d) => configure(d));

    void IDescriptorExtension.OnBeforeCreate(
        Action<IDescriptorContext, DefinitionBase> configure)
        => OnBeforeCreate(configure);

    private void OnBeforeCreate(Action<IDescriptorContext, T> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        Definition.Configurations.Add(new CreateConfiguration(
            (c, d) => configure(c, (T)d),
            Definition));
    }

    INamedDependencyDescriptor IDescriptorExtension<T>.OnBeforeNaming(
        Action<ITypeCompletionContext, T> configure)
        => OnBeforeNaming(configure);

    INamedDependencyDescriptor IDescriptorExtension.OnBeforeNaming(
        Action<ITypeCompletionContext, DefinitionBase> configure)
        => OnBeforeNaming(configure);

    private INamedDependencyDescriptor OnBeforeNaming(
        Action<ITypeCompletionContext, T> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var configuration = new CompleteConfiguration(
            (c, d) => configure(c, (T)d),
            Definition,
            ApplyConfigurationOn.BeforeNaming);

        Definition.Configurations.Add(configuration);

        return new NamedDependencyDescriptor(Context.TypeInspector, configuration);
    }

    ICompletedDependencyDescriptor IDescriptorExtension<T>.OnBeforeCompletion(
        Action<ITypeCompletionContext, T> configure)
        => OnBeforeCompletion(configure);

    ICompletedDependencyDescriptor IDescriptorExtension.OnBeforeCompletion(
        Action<ITypeCompletionContext, DefinitionBase> configure)
        => OnBeforeCompletion(configure);

    private ICompletedDependencyDescriptor OnBeforeCompletion(
        Action<ITypeCompletionContext, T> configure)
    {
        var configuration = new CompleteConfiguration(
            (c, d) => configure(c, (T)d),
            Definition,
            ApplyConfigurationOn.BeforeCompletion);

        Definition.Configurations.Add(configuration);

        return new CompletedDependencyDescriptor(Context.TypeInspector, configuration);
    }
}
