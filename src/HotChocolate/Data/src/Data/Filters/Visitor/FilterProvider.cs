using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using static HotChocolate.Data.DataResources;
using static HotChocolate.Data.ThrowHelper;
using static HotChocolate.Data.ErrorHelper;

namespace HotChocolate.Data.Filters
{
    public abstract class FilterProvider<TContext>
        : Convention<FilterProviderDefinition>,
          IFilterProvider,
          IFilterProviderConvention
        where TContext : IFilterVisitorContext
    {
        private readonly List<IFilterFieldHandler<TContext>> _fieldHandlers =
            new List<IFilterFieldHandler<TContext>>();

        private Action<IFilterProviderDescriptor<TContext>>? _configure;

        protected FilterProvider()
        {
            _configure = Configure;
        }

        public FilterProvider(Action<IFilterProviderDescriptor<TContext>> configure)
        {
            _configure = configure ??
                throw new ArgumentNullException(nameof(configure));
        }

        public IReadOnlyCollection<IFilterFieldHandler> FieldHandlers => _fieldHandlers;

        public void Initialize(IConventionContext context)
        {
            base.Initialize(context);
        }

        protected override FilterProviderDefinition CreateDefinition(IConventionContext context)
        {
            if (_configure is null)
            {
                throw new InvalidOperationException(FilterProvider_NoConfigurationSpecified);
            }

            var descriptor = FilterProviderDescriptor<TContext>.New();

            _configure(descriptor);
            _configure = null;

            return descriptor.CreateDefinition();
        }

        protected override void OnComplete(
            IConventionContext context,
            FilterProviderDefinition definition)
        {
            if (definition.Handlers.Count == 0)
            {
                throw FilterProvider_NoHandlersConfigured(this);
            }

            IServiceProvider services = new DictionaryServiceProvider(
                    (typeof(IFilterProvider), this),
                    (typeof(IConventionContext), context),
                    (typeof(IDescriptorContext), context.DescriptorContext),
                    (typeof(ITypeInspector), context.DescriptorContext.TypeInspector))
                .Include(context.Services);

            foreach ((Type Type, IFilterFieldHandler? Instance) handler in definition.Handlers)
            {
                switch (handler.Instance)
                {
                    case null when services.TryGetOrCreateService(
                        handler.Type,
                        out IFilterFieldHandler<TContext>? service):
                        _fieldHandlers.Add(service);
                        break;
                    case null:
                        context.ReportError(
                            FilterProvider_UnableToCreateFieldHandler(this, handler.Type));
                        break;
                    case IFilterFieldHandler<TContext> casted:
                        _fieldHandlers.Add(casted);
                        break;
                }
            }
        }

        protected virtual void Configure(IFilterProviderDescriptor<TContext> descriptor) { }

        public abstract FieldMiddleware CreateExecutor<TEntityType>(NameString argumentName);

        public virtual void ConfigureField(
            NameString argumentName,
            IObjectFieldDescriptor descriptor)
        {
        }
    }
}
