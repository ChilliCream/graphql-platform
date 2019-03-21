using System.Reflection.Metadata;
using System;
using System.Collections.Generic;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Configuration;

namespace HotChocolate.Types
{
    public abstract class TypeSystemObjectBase<TDefinition>
        : TypeSystemObjectBase
        where TDefinition : DefinitionBase
    {
        private TDefinition _definition;
        private Dictionary<string, object> _contextData;

        protected TypeSystemObjectBase() { }

        public override IReadOnlyDictionary<string, object> ContextData =>
            _contextData;

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

        internal sealed override void CompleteType(ICompletionContext context)
        {
            Description = _definition.Description;

            OnCompleteType(context, _definition);

            _contextData = new Dictionary<string, object>(
                _definition.ContextData);
            _definition = null;

            base.CompleteType(context);
        }

        protected virtual void OnCompleteType(
            ICompletionContext context,
            TDefinition definition)
        {
        }
    }
}
