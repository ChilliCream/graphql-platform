using System.Reflection.Metadata;
using System;
using System.Collections.Generic;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public abstract class TypeSystemObjectBase<TDefinition>
        : TypeSystemObjectBase
        where TDefinition : DefinitionBase
    {
        private TDefinition _definition;

        protected TypeSystemObjectBase() { }

        internal sealed override void Initialize(IInitializationContext context)
        {
            _definition = CreateDefinition(context);
            if (_definition == null)
            {
                // TODO : exception type
                // TODO : resources
                throw new InvalidOperationException();
            }
            OnRegisterDependencies(context, _definition);
            base.Initialize(context);
        }

        protected abstract TDefinition CreateDefinition(
            IInitializationContext context);

        protected virtual void OnRegisterDependencies(
            IInitializationContext context,
            TDefinition definition)
        {
        }

        internal sealed override void CompleteName(ICompletionContext context)
        {
            OnCompleteName(context, _definition);
            base.CompleteName(context);
        }

        protected virtual void OnCompleteName(
            ICompletionContext context,
            TDefinition definition)
        {
            if (definition.Name.IsEmpty)
            {
                // TODO : exception type
                // TODO : resources
                throw new InvalidOperationException(
                    "The type is initialize bla bla ...");
            }

            Name = definition.Name;
        }

        internal sealed override void CompleteObject(ICompletionContext context)
        {
            OnCompleteObject(context, _definition);
            base.CompleteObject(context);
            _definition = null;
        }

        protected virtual void OnCompleteObject(
            ICompletionContext context,
            TDefinition definition)
        {
        }
    }
}
