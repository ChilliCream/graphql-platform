using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;

namespace HotChocolate.Data.Filters
{
    public abstract class FilterProvider<T, TContext>
        : FilterProviderBase<FilterProviderDefiniton<T, TContext>>
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
            IFilterProviderInitializationContext context)
        {
            var descriptor = FilterProviderDescriptor<T, TContext>.New(context);
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected override void OnComplete(
            IFilterProviderInitializationContext context,
            FilterProviderDefiniton<T, TContext>? definition)
        {
            if (definition is { } def)
            {
                Handlers = def.Handlers.ToArray();
                Visitor = def.Visitor ??
                    throw ThrowHelper.FilterConvention_NoVisitor(def.Scope);

                if (def.Combinator is null)
                {
                    throw ThrowHelper.FilterConvention_NoCombinatorFound(def.Scope);
                }
                else if (def.Combinator is FilterOperationCombinator<T, TContext> combiantorOfT)
                {
                    Visitor.Combinator = combiantorOfT;
                }
                else
                {
                    throw ThrowHelper.FilterConvention_NoCombinatorFound(def.Scope);
                }

                IFilterFieldHandlerInitializationContext? handlerContext =
                    FilterFieldHandlerInitializationContext.From(context, this);

                foreach (FilterFieldHandler<T, TContext>? handler in Handlers)
                {
                    handler.Initialize(handlerContext);
                }
            }
        }

        protected virtual void Configure(IFilterProviderDescriptor<T, TContext> descriptor) { }

        public override bool TryGetHandler(
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
