using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    public abstract class DescriptorBase<T>
        : IDescriptor<T>
        , IDescriptorExtension<T>
        , IDescriptorExtension
        , IDefinitionFactory<T>
        where T : DefinitionBase
    {
        private List<Action<IDescriptorContext, T>>? _modifiers;

        protected DescriptorBase(IDescriptorContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        protected internal IDescriptorContext Context { get; }

        IDescriptorContext IHasDescriptorContext.Context => Context;

        protected internal abstract T Definition { get; protected set; }

        public IDescriptorExtension<T> Extend()
        {
            return this;
        }

        public T CreateDefinition()
        {
            OnCreateDefinition(Definition);

            if (_modifiers is not null)
            {
                foreach (Action<IDescriptorContext, T> modifier in _modifiers)
                {
                    modifier.Invoke(Context, Definition);
                }
            }

            return Definition;
        }

        public void ConfigureContextData(Action<ExtensionData> configure)
        {
            configure(Definition.ContextData);
        }

        protected virtual void OnCreateDefinition(T definition)
        {
        }

        DefinitionBase IDefinitionFactory.CreateDefinition() =>
            CreateDefinition();

        void IDescriptorExtension<T>.OnBeforeCreate(
            Action<T> configure) =>
            OnBeforeCreate((c, d) => configure(d));

        void IDescriptorExtension<T>.OnBeforeCreate(
            Action<IDescriptorContext, T> configure) =>
            OnBeforeCreate(configure);

        void IDescriptorExtension.OnBeforeCreate(
            Action<DefinitionBase> configure) =>
            OnBeforeCreate((c, d) => configure(d));

        void IDescriptorExtension.OnBeforeCreate(
            Action<IDescriptorContext, DefinitionBase> configure) =>
            OnBeforeCreate(configure);

        private void OnBeforeCreate(Action<IDescriptorContext, T> configure)
        {
            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            _modifiers ??= new List<Action<IDescriptorContext, T>>();

            _modifiers.Add(configure);
        }

        INamedDependencyDescriptor IDescriptorExtension<T>.OnBeforeNaming(
            Action<ITypeCompletionContext, T> configure) =>
            OnBeforeNaming(configure);

        INamedDependencyDescriptor IDescriptorExtension.OnBeforeNaming(
            Action<ITypeCompletionContext, DefinitionBase> configure) =>
            OnBeforeNaming(configure);

        private INamedDependencyDescriptor OnBeforeNaming(
            Action<ITypeCompletionContext, T> configure)
        {
            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var configuration = new TypeConfiguration<T>
            {
                Definition = Definition,
                On = ApplyConfigurationOn.Naming,
                Configure = configure
            };

            Definition.Configurations.Add(configuration);

            return new NamedDependencyDescriptor<T>(Context.TypeInspector, configuration);
        }

        ICompletedDependencyDescriptor IDescriptorExtension<T>.OnBeforeCompletion(
            Action<ITypeCompletionContext, T> configure) =>
            OnBeforeCompletion(configure);

        ICompletedDependencyDescriptor IDescriptorExtension.OnBeforeCompletion(
            Action<ITypeCompletionContext, DefinitionBase> configure) =>
            OnBeforeCompletion(configure);

        private ICompletedDependencyDescriptor OnBeforeCompletion(
            Action<ITypeCompletionContext, T> configure)
        {
            var configuration = new TypeConfiguration<T>
            {
                Definition = Definition,
                On = ApplyConfigurationOn.Completion,
                Configure = configure
            };
            Definition.Configurations.Add(configuration);

            return new CompletedDependencyDescriptor<T>(Context.TypeInspector, configuration);
        }
    }
}
