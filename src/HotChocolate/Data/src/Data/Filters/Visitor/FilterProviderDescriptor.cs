using System;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Filters
{
    public class FilterProviderDescriptor<T, TContext>
        : IFilterProviderDescriptor<T, TContext>
        where TContext : FilterVisitorContext<T>
    {
        private readonly IServiceProvider _services;

        protected FilterProviderDescriptor(IConventionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Definition.Scope = context.Scope;
            _services = context.Services;
        }

        protected FilterProviderDefiniton<T, TContext> Definition { get; set; } =
            new FilterProviderDefiniton<T, TContext>();

        public FilterProviderDefiniton<T, TContext> CreateDefinition()
        {
            return Definition;
        }

        public static FilterProviderDescriptor<T, TContext> New(
            IConventionContext context) =>
            new FilterProviderDescriptor<T, TContext>(context);

        public IFilterProviderDescriptor<T, TContext> AddFieldHandler<TFieldHandler>()
            where TFieldHandler : FilterFieldHandler<T, TContext>
        {
            Definition.Handlers.Add(_services.GetService<TFieldHandler>());
            return this;
        }

        public IFilterProviderDescriptor<T, TContext> AddFieldHandler<TFieldHandler>(
            TFieldHandler handler)
            where TFieldHandler : FilterFieldHandler<T, TContext>
        {
            Definition.Handlers.Add(handler);
            return this;
        }

        public IFilterProviderDescriptor<T, TContext> Visitor<TVisitor>()
            where TVisitor : FilterVisitor<T, TContext>
        {
            Definition.Visitor = _services.GetService<TVisitor>();
            return this;
        }

        public IFilterProviderDescriptor<T, TContext> Visitor<TVisitor>(TVisitor handler)
            where TVisitor : FilterVisitor<T, TContext>
        {
            Definition.Visitor = handler;
            return this;
        }

        public IFilterProviderDescriptor<T, TContext> Combinator<TCombinator>(TCombinator handler)
            where TCombinator : FilterOperationCombinator
        {
            Definition.Combinator = handler;
            return this;
        }

        public IFilterProviderDescriptor<T, TContext> Combinator<TCombinator>()
            where TCombinator : FilterOperationCombinator
        {
            Definition.Combinator = _services.GetService<TCombinator>();
            return this;
        }
    }
}
