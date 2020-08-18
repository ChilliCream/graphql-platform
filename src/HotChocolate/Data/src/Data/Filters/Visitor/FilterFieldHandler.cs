using System;
using System.Diagnostics;
using HotChocolate.Configuration;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public class FilterFieldHandler : IHasScope
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
        public abstract bool CanHandle(
            ITypeDiscoveryContext context,
            FilterInputTypeDefinition typeDefinition,
            FilterFieldDefinition fieldDefinition);

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
