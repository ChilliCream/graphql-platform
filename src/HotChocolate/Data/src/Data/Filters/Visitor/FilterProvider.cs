using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters
{
    public class FilterProvider<T, TContext>
        : ConventionBase<FilterProviderDefiniton<T, TContext>>
        , IFilterProvider
        where TContext : FilterVisitorContext<T>
    {
        private readonly Action<IFilterProviderDescriptor<T, TContext>> _configure;

        public IReadOnlyCollection<FilterFieldHandler<T, TContext>> Handlers { get; private set; } =
            Array.Empty<FilterFieldHandler<T, TContext>>();

        public FilterVisitor<T, TContext> Visitor { get; private set; }

        protected FilterProvider()
        {
            _configure = Configure;
        }

        public FilterProvider(Action<IFilterProviderDescriptor<T, TContext>> configure)
        {
            _configure = configure ??
                throw new ArgumentNullException(nameof(configure));
        }

        protected override FilterProviderDefiniton<T, TContext>? CreateDefinition(
            IConventionContext context)
        {
            var descriptor = FilterProviderDescriptor<T, TContext>.New(context);
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected override void OnComplete(
            IConventionContext context,
            FilterProviderDefiniton<T, TContext>? definition)
        {
            if (definition is { } def)
            {
                Handlers = def.Handlers.ToArray();
                Visitor = def.Visitor ??
                    throw ThrowHelper.FilterConvention_NoVisitor(def.Scope);
                Visitor.Combinator = def.Combinator ??
                    throw ThrowHelper.FilterConvention_NoCombinatorFound(def.Scope);
            }
        }

        protected virtual void Configure(IFilterProviderDescriptor<T, TContext> descriptor) { }

        public bool TryGetHandler(
            ITypeDiscoveryContext context,
            FilterInputTypeDefinition typeDefinition,
            FilterFieldDefinition fieldDefinition,
            [NotNullWhen(true)] out FilterFieldHandler? handler)
        {
            foreach (FilterFieldHandler<T, TContext>? currentHandler in Handlers)
            {
                if (currentHandler.CanHandle(context, typeDefinition, fieldDefinition))
                {
                    handler = currentHandler;
                    return true;
                }
            }
            handler = default;
            return false;
        }
    }
}

