using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using static HotChocolate.Data.DataResources;
using static HotChocolate.Data.ThrowHelper;
using static HotChocolate.Data.ErrorHelper;

namespace HotChocolate.Data.Sorting
{
    public abstract class SortProvider<TContext>
        : Convention<SortProviderDefinition>,
          ISortProvider,
          ISortProviderConvention
        where TContext : ISortVisitorContext
    {
        private readonly List<ISortFieldHandler<TContext>> _fieldHandlers =
            new List<ISortFieldHandler<TContext>>();

        private readonly List<ISortOperationHandler<TContext>> _operationHandlers =
            new List<ISortOperationHandler<TContext>>();

        private Action<ISortProviderDescriptor<TContext>>? _configure;

        protected SortProvider()
        {
            _configure = Configure;
        }

        public SortProvider(Action<ISortProviderDescriptor<TContext>> configure)
        {
            _configure = configure ??
                throw new ArgumentNullException(nameof(configure));
        }

        internal new SortProviderDefinition? Definition => base.Definition;

        public IReadOnlyCollection<ISortFieldHandler> FieldHandlers => _fieldHandlers;

        public IReadOnlyCollection<ISortOperationHandler> OperationHandlers => _operationHandlers;

        public new void Initialize(IConventionContext context)
        {
            base.Initialize(context);
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

        void ISortProviderConvention.Initialize(IConventionContext context)
        {
            base.Initialize(context);
        }

        void ISortProviderConvention.OnComplete(IConventionContext context)
        {
            OnComplete(context);
        }

        protected override void OnComplete(IConventionContext context)
        {
            if (Definition?.Handlers.Count == 0)
            {
                throw SortProvider_NoFieldHandlersConfigured(this);
            }

            if (Definition.OperationHandlers.Count == 0)
            {
                throw SortProvider_NoOperationHandlersConfigured(this);
            }

            IServiceProvider services = new DictionaryServiceProvider(
                (typeof(ISortProvider), this),
                (typeof(IConventionContext), context),
                (typeof(IDescriptorContext), context.DescriptorContext),
                (typeof(ITypeInspector), context.DescriptorContext.TypeInspector))
                .Include(context.Services);

            foreach ((Type Type, ISortFieldHandler? Instance) handler in Definition.Handlers)
            {
                switch (handler.Instance)
                {
                    case null when services.TryGetOrCreateService(
                        handler.Type,
                        out ISortFieldHandler<TContext>? service):
                        _fieldHandlers.Add(service);
                        break;

                    case null:
                        throw new SchemaException(
                            SortProvider_UnableToCreateFieldHandler(this, handler.Type));

                    case ISortFieldHandler<TContext> casted:
                        _fieldHandlers.Add(casted);
                        break;
                }
            }

            foreach ((Type Type, ISortOperationHandler? Instance) handler
                in Definition.OperationHandlers)
            {
                switch (handler.Instance)
                {
                    case null when services.TryGetOrCreateService(
                        handler.Type,
                        out ISortOperationHandler<TContext>? service):
                        _operationHandlers.Add(service);
                        break;

                    case null:
                        throw new SchemaException(
                            SortProvider_UnableToCreateOperationHandler(this, handler.Type));

                    case ISortOperationHandler<TContext> casted:
                        _operationHandlers.Add(casted);
                        break;
                }
            }
        }

        protected virtual void Configure(ISortProviderDescriptor<TContext> descriptor) { }

        public abstract FieldMiddleware CreateExecutor<TEntityType>(NameString argumentName);
    }
}
