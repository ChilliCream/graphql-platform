using System;

namespace HotChocolate.Data.Filters
{
    public class FilterFieldHandlerInitializationContext
        : FilterProviderInitializationContext,
        IFilterFieldHandlerInitializationContext
    {
        public FilterFieldHandlerInitializationContext(
            string? scope,
            IServiceProvider services,
            IFilterConvention convention,
            IFilterProvider provider)
            : base(scope, services, convention)
        {
            Provider = provider;
        }

        public IFilterProvider Provider { get; }

        public new static IFilterFieldHandlerInitializationContext From(
            IFilterProviderInitializationContext context,
            IFilterProvider provider) =>
            new FilterFieldHandlerInitializationContext(
                context.Scope, context.Services, context.Convention, provider);
    }
}
