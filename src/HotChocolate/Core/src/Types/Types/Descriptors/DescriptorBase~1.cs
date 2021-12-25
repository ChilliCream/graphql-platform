using System;
using System.Buffers;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Descriptors;

public abstract class DescriptorBase<T>
    : IDescriptor<T>
    , IDescriptorExtension<T>
    , IDescriptorExtension
    , IDefinitionFactory<T>
    where T : DefinitionBase
{
    protected DescriptorBase(IDescriptorContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    protected internal IDescriptorContext Context { get; }

    IDescriptorContext IHasDescriptorContext.Context => Context;

    protected internal abstract T Definition { get; protected set; }

    T IDescriptorExtension<T>.Definition => Definition;

    public IDescriptorExtension<T> Extend() => this;

    public T CreateDefinition()
    {
        OnCreateDefinition(Definition);

        if (Definition.HasConfigurations)
        {
            var i = 0;
            var buffered = 0;
            var length = Definition.Configurations.Count;
            CreateConfiguration[] rented = ArrayPool<CreateConfiguration>.Shared.Rent(length);
            IList<ITypeSystemMemberConfiguration> configurations = Definition.Configurations;

            do
            {
                if (configurations[i] is { On: ApplyConfigurationOn.Create } config)
                {
                    configurations.RemoveAt(i);
                    rented[buffered++] = (CreateConfiguration)config;
                }
                else
                {
                    i++;
                }
            } while (i < configurations.Count);

            for (i = 0; i < buffered; i++)
            {
                rented[i].Configure(Context);
            }

            rented.AsSpan().Slice(0, length).Clear();
            ArrayPool<CreateConfiguration>.Shared.Return(rented, true);
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
            ApplyConfigurationOn.Naming);

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
            ApplyConfigurationOn.Completion);

        Definition.Configurations.Add(configuration);

        return new CompletedDependencyDescriptor(Context.TypeInspector, configuration);
    }
}
