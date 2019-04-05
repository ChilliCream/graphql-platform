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

        void IDescriptorExtension<T>.OnBeforeCreate(Action<T> configure)
        {
            _modifiers.Add(configure);
        }

        IOnBeforeNamingDescriptor IDescriptorExtension<T>.OnBeforeNaming(
            Action<ICompletionContext, T> configure)
        {
            throw new NotImplementedException();
        }

        IOnBeforeCompletionDescriptor IDescriptorExtension<T>.OnBeforeCompletion(
            Action<ICompletionContext, T> configure)
        {
            throw new NotImplementedException();
        }
    }
}
