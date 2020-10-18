using System;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Data.DataResources;

namespace HotChocolate.Data.Sorting
{
    public abstract class SortProviderExtensions<TContext>
        : ConventionExtension<SortProviderDefinition>,
          ISortProviderExtension,
          ISortProviderConvention
        where TContext : ISortVisitorContext
    {
        private Action<ISortProviderDescriptor<TContext>>? _configure;

        protected SortProviderExtensions()
        {
            _configure = Configure;
        }

        public SortProviderExtensions(Action<ISortProviderDescriptor<TContext>> configure)
        {
            _configure = configure ??
                throw new ArgumentNullException(nameof(configure));
        }

        void ISortProviderConvention.Initialize(IConventionContext context)
        {
            base.Initialize(context);
        }

        void ISortProviderConvention.OnComplete(IConventionContext context)
        {
            OnComplete(context);
        }

        protected override SortProviderDefinition CreateDefinition(IConventionContext context)
        {
            if (_configure is null)
            {
                throw new InvalidOperationException(SortProvider_NoConfigurationSpecified);
            }

            var descriptor = SortProviderDescriptor<TContext>.New();

            _configure(descriptor);
            _configure = null;

            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(ISortProviderDescriptor<TContext> descriptor) { }

        public override void Merge(IConventionContext context, Convention convention)
        {
            if (Definition is {} &&
                convention is SortProvider<TContext> conv &&
                conv.Definition is {} target)
            {
                // Provider extensions should be applied by default before the default handlers, as
                // the interceptor picks up the first handler. A provider extension should adds more
                // specific handlers than the default providers
                for (var i = Definition.Handlers.Count - 1; i >= 0 ; i--)
                {
                    target.Handlers.Insert(0, Definition.Handlers[i]);
                }

                for (var i = Definition.OperationHandlers.Count - 1; i >= 0 ; i--)
                {
                    target.OperationHandlers.Insert(0, Definition.OperationHandlers[i]);
                }
            }
        }
    }
}
