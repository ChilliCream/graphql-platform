using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Descriptors;

public abstract class DescriptorBase<T>(IDescriptorContext context)
    : IDescriptor<T>
    , IDescriptorExtension<T>
    , IDescriptorExtension
    , IConfigurationFactory<T>
    where T : TypeSystemConfiguration
{
    protected internal IDescriptorContext Context { get; } =
        context ?? throw new ArgumentNullException(nameof(context));

    IDescriptorContext IHasDescriptorContext.Context => Context;

    protected internal abstract T Configuration { get; protected set; }

    T IDescriptorExtension<T>.Configuration => Configuration;

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

    public IDescriptorExtension<T> ExtendWith<TState>(
        Action<IDescriptorExtension<T>, TState> configure,
        TState state)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        configure(this, state);
        return this;
    }


    public T CreateConfiguration()
    {
        OnCreateDefinition(Configuration);

        if (Configuration.HasTasks)
        {
            var i = 0;
            var configurations = Configuration.Tasks;

            do
            {
                if (configurations[i] is { On: ApplyConfigurationOn.Create } config)
                {
                    configurations.RemoveAt(i);
                    ((OnCreateTypeSystemConfigurationTask)config).Configure(Context);
                }
                else
                {
                    i++;
                }
            } while (i < configurations.Count);
        }

        return Configuration;
    }

    protected virtual void OnCreateDefinition(T definition)
    {
    }

    TypeSystemConfiguration IConfigurationFactory.CreateConfiguration()
        => CreateConfiguration();

    void IDescriptorExtension<T>.OnBeforeCreate(
        Action<T> configure)
        => OnBeforeCreate((_, d) => configure(d));

    void IDescriptorExtension<T>.OnBeforeCreate(
        Action<IDescriptorContext, T> configure)
        => OnBeforeCreate(configure);

    void IDescriptorExtension.OnBeforeCreate(
        Action<TypeSystemConfiguration> configure)
        => OnBeforeCreate((_, d) => configure(d));

    void IDescriptorExtension.OnBeforeCreate(
        Action<IDescriptorContext, TypeSystemConfiguration> configure)
        => OnBeforeCreate(configure);

    private void OnBeforeCreate(Action<IDescriptorContext, T> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        Configuration.Tasks.Add(new OnCreateTypeSystemConfigurationTask(
            (c, d) => configure(c, (T)d),
            Configuration));
    }

    INamedDependencyDescriptor IDescriptorExtension<T>.OnBeforeNaming(
        Action<ITypeCompletionContext, T> configure)
        => OnBeforeNaming(configure);

    INamedDependencyDescriptor IDescriptorExtension.OnBeforeNaming(
        Action<ITypeCompletionContext, TypeSystemConfiguration> configure)
        => OnBeforeNaming(configure);

    private INamedDependencyDescriptor OnBeforeNaming(
        Action<ITypeCompletionContext, T> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var configuration = new OnCompleteTypeSystemConfigurationTask(
            (c, d) => configure(c, (T)d),
            Configuration,
            ApplyConfigurationOn.BeforeNaming);

        Configuration.Tasks.Add(configuration);

        return new NamedDependencyDescriptor(Context.TypeInspector, configuration);
    }

    ICompletedDependencyDescriptor IDescriptorExtension<T>.OnBeforeCompletion(
        Action<ITypeCompletionContext, T> configure)
        => OnBeforeCompletion(configure);

    ICompletedDependencyDescriptor IDescriptorExtension.OnBeforeCompletion(
        Action<ITypeCompletionContext, TypeSystemConfiguration> configure)
        => OnBeforeCompletion(configure);

    private ICompletedDependencyDescriptor OnBeforeCompletion(
        Action<ITypeCompletionContext, T> configure)
    {
        var configuration = new OnCompleteTypeSystemConfigurationTask(
            (c, d) => configure(c, (T)d),
            Configuration,
            ApplyConfigurationOn.BeforeCompletion);

        Configuration.Tasks.Add(configuration);

        return new CompletedDependencyDescriptor(Context.TypeInspector, configuration);
    }
}
