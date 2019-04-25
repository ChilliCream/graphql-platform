using System;
using HotChocolate.Types.Descriptors.Definitions;
using System.Collections.Generic;
using HotChocolate.Configuration;

namespace HotChocolate.Types.Descriptors
{
    public abstract class DescriptorBase<T>
        : IDescriptor<T>
        , IDescriptorExtension<T>
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

        protected IDescriptorContext Context { get; }

        IDescriptorContext IHasDescriptorContext.Context => Context;

        protected abstract T Definition { get; }

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

        void IDescriptorExtension<T>.OnBeforeCreate(Action<T> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            _modifiers.Add(configure);
        }

        INamedDependencyDescriptor IDescriptorExtension<T>.OnBeforeNaming(
            Action<ICompletionContext, T> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var configuration = new TypeConfiguration<T>();
            configuration.Definition = Definition;
            configuration.Kind = ConfigurationKind.Naming;
            configuration.Configure = configure;
            Definition.Configurations.Add(configuration);

            return new NamedDependencyDescriptor<T>(configuration);
        }

        ICompletedDependencyDescriptor IDescriptorExtension<T>
            .OnBeforeCompletion(
                Action<ICompletionContext, T> configure)
        {
            var configuration = new TypeConfiguration<T>();
            configuration.Definition = Definition;
            configuration.Kind = ConfigurationKind.Completion;
            configuration.Configure = configure;
            Definition.Configurations.Add(configuration);

            return new CompletedDependencyDescriptor<T>(configuration);
        }
    }
}
