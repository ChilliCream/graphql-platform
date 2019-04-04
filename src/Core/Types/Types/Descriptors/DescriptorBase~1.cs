using System;
using HotChocolate.Types.Descriptors.Definitions;
using System.Collections.Generic;

namespace HotChocolate.Types.Descriptors
{
    public abstract class DescriptorBase<T>
        : IDescriptor<T>
        , IDefinitionFactory<T>
        , IHasDescriptorContext
        where T : DefinitionBase
    {
        protected DescriptorBase(IDescriptorContext context)
        {
            Context = context
                ?? throw new ArgumentNullException(nameof(context));
        }

        protected IDescriptorContext Context { get; }

        IDescriptorContext IHasDescriptorContext.Context => Context;

        protected abstract T Definition { get; }

        public void Configure(Action<T> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            Configure(new TypeConfiguration<T>(configure));
        }

        public void Configure(TypeConfiguration<T> configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            Definition.Configurations.Add(configuration);
        }

        public T CreateDefinition()
        {
            OnCreateDefinition(Definition);
            return Definition;
        }

        protected virtual void OnCreateDefinition(T definition)
        {
        }

        DefinitionBase IDefinitionFactory.CreateDefinition() =>
            CreateDefinition();
    }
}
