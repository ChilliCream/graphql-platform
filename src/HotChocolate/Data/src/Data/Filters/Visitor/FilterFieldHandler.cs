using System;
using System.Diagnostics;
using HotChocolate.Configuration;

namespace HotChocolate.Data.Filters
{
    public class FilterFieldHandler
    {
        private FilterFieldHandlerStatus _status = FilterFieldHandlerStatus.Uninitialized;

        public string? Scope { get; private set; }

        public IFilterProvider Provider { get; private set; } = null!;

        public IFilterConvention Convention { get; private set; } = null!;

        public virtual void Initialize(IFilterFieldHandlerInitializationContext context)
        {
            Scope = context.Scope;
            Provider = context.Provider;
            Convention = context.Convention;
            OnComplete(context);
            MarkInitialized();
        }
        public virtual bool CanHandle(
            ITypeDiscoveryContext context,
            FilterInputTypeDefinition typeDefinition,
            FilterFieldDefinition fieldDefinition) => false;

        protected virtual void OnComplete(IFilterFieldHandlerInitializationContext context)
        {

        }

        protected void MarkInitialized()
        {
            Debug.Assert(_status == FilterFieldHandlerStatus.Uninitialized);

            if (_status != FilterFieldHandlerStatus.Uninitialized)
            {
                throw new InvalidOperationException();
            }

            _status = FilterFieldHandlerStatus.Initialized;
        }
    }
}
