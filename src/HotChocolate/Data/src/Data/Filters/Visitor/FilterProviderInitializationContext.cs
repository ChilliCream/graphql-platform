using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters
{
    public class FilterProviderInitializationContext
        : ConventionContext,
        IFilterProviderInitializationContext
    {
        public FilterProviderInitializationContext(
            string? scope,
            IServiceProvider services,
            IFilterConvention convention)
            : base(scope, services)
        {
            Convention = convention;
        }

        public IFilterConvention Convention { get; }

        public static IFilterProviderInitializationContext From(
            IConventionContext context,
            IFilterConvention convention) =>
            new FilterProviderInitializationContext(context.Scope, context.Services, convention);
    }
}
