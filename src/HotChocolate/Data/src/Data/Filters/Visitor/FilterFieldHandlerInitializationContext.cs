using System;

namespace HotChocolate.Data.Filters
{
    public class FilterFieldHandlerInitializationContext
        : FilterProviderInitializationContext,
        IFilterFieldHandlerInitializationContext
    {
        public FilterFieldHandlerInitializationContext(
            IServiceProvider services,
            IFilterConvention convention,
            IFilterProvider provider,
            string? scope)
            : base(scope, services, convention)
        {
            Provider = provider;
        }

        public IFilterProvider Provider { get; }

        public static IFilterFieldHandlerInitializationContext From(
            IFilterProviderInitializationContext context,
            IFilterProvider provider) =>
            new FilterFieldHandlerInitializationContext(
                context.Services, context.Convention, provider,context.Scope);
    }
}
