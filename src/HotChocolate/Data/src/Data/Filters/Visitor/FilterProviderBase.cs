using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Filters
{
    public abstract class FilterProviderBase
        : IFilterProvider
    {
        private FilterProviderStatus _status = FilterProviderStatus.Uninitialized;

        public string? Scope { get; set; }

        public IFilterConvention Convention { get; set; } = null!;

        public abstract Task ExecuteAsync<TEntityType>(
            FieldDelegate next,
            IMiddlewareContext context);

        public virtual void Initialize(IFilterProviderInitializationContext context)
        {
            Scope = context.Scope;
            Convention = context.Convention;
            MarkInitialized();
        }

        public abstract bool TryGetHandler(
            ITypeDiscoveryContext context,
            FilterInputTypeDefinition typeDefinition,
            FilterFieldDefinition fieldDefinition,
            [NotNullWhen(true)] out FilterFieldHandler? handler);

        protected void MarkInitialized()
        {
            Debug.Assert(_status == FilterProviderStatus.Uninitialized);

            if (_status != FilterProviderStatus.Uninitialized)
            {
                throw new InvalidOperationException();
            }

            _status = FilterProviderStatus.Initialized;
        }
    }
}
