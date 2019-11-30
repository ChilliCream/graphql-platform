using System;
using HotChocolate.Types.Descriptors.Definitions;
using System.Collections.Generic;
using HotChocolate.Configuration;

namespace HotChocolate.Types.Descriptors
{
    public abstract class DescriptorBase<T>
        : IDescriptor<T>
        , IDescriptorExtension<T>
        , IDescriptorExtension
        , IDefinitionFactory<T>
        , IHasDescriptorContext
        where T : DefinitionBase
    {
        private List<Action<T>> _modifiers = new List<Action<T>>();

        protected DescriptorBase(IDescriptorContext context)
        {
            Context = context
                ?? throw new ArgumentNullException(nameof(context));
        }

        protected internal IDescriptorContext Context { get; }

        IDescriptorContext IHasDescriptorContext.Context => Context;

        internal protected abstract T Definition { get; }

        public IDescriptorExtension<T> Extend()
        {
            return this;
        }

        public T CreateDefinition()
        {
            OnCreateDefinition(Definition);

            foreach (Action<T> modifier in _modifiers)
            {
                modifier.Invoke(Definition);
            }

            return Definition;
        }

        protected virtual void OnCreateDefinition(T definition)
        {
        }

        DefinitionBase IDefinitionFactory.CreateDefinition() =>
            CreateDefinition();

        void IDescriptorExtension<T>.OnBeforeCreate(Action<T> configure) =>
            OnBeforeCreate(configure);

        void IDescriptorExtension.OnBeforeCreate(Action<DefinitionBase> configure) =>
            OnBeforeCreate(c => configure(c));

        private void OnBeforeCreate(Action<T> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            _modifiers.Add(configure);
        }

        INamedDependencyDescriptor IDescriptorExtension<T>.OnBeforeNaming(
            Action<ICompletionContext, T> configure) =>
            OnBeforeNaming(configure);

        INamedDependencyDescriptor IDescriptorExtension.OnBeforeNaming(
            Action<ICompletionContext, DefinitionBase> configure) =>
            OnBeforeNaming(configure);

        private INamedDependencyDescriptor OnBeforeNaming(
           Action<ICompletionContext, T> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var configuration = new TypeConfiguration<T>();
            configuration.Definition = Definition;
            configuration.On = ApplyConfigurationOn.Naming;
            configuration.Configure = configure;
            Definition.Configurations.Add(configuration);

            return new NamedDependencyDescriptor<T>(configuration);
        }

        ICompletedDependencyDescriptor IDescriptorExtension<T>.OnBeforeCompletion(
            Action<ICompletionContext, T> configure) =>
            OnBeforeCompletion(configure);

        ICompletedDependencyDescriptor IDescriptorExtension.OnBeforeCompletion(
            Action<ICompletionContext, DefinitionBase> configure) =>
            OnBeforeCompletion(configure);

        private ICompletedDependencyDescriptor OnBeforeCompletion(
            Action<ICompletionContext, T> configure)
        {
            var configuration = new TypeConfiguration<T>();
            configuration.Definition = Definition;
            configuration.On = ApplyConfigurationOn.Completion;
            configuration.Configure = configure;
            Definition.Configurations.Add(configuration);

            return new CompletedDependencyDescriptor<T>(configuration);
        }
    }
}
