using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using static HotChocolate.Data.DataResources;
using static HotChocolate.Data.ThrowHelper;

namespace HotChocolate.Data.Sorting
{
    /// <summary>
    /// A <see cref="SortProvider{TContext}"/> translates a incoming query to another
    /// object structure at runtime
    /// </summary>
    /// <typeparam name="TContext">The type of the context</typeparam>
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

        /// <inheritdoc />
        public IReadOnlyCollection<ISortFieldHandler> FieldHandlers => _fieldHandlers;

        /// <inheritdoc />
        public IReadOnlyCollection<ISortOperationHandler> OperationHandlers => _operationHandlers;

        public new void Initialize(IConventionContext context)
        {
            base.Initialize(context);
        }

        /// <inheritdoc />
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

        void ISortProviderConvention.Complete(IConventionContext context)
        {
            Complete(context);
        }

        /// <inheritdoc />
        protected override void Complete(IConventionContext context)
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
                        throw SortProvider_UnableToCreateFieldHandler(this, handler.Type);

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
                        throw SortProvider_UnableToCreateOperationHandler(this, handler.Type);

                    case ISortOperationHandler<TContext> casted:
                        _operationHandlers.Add(casted);
                        break;
                }
            }
        }

        /// <summary>
        /// This method is called on initialization of the provider but before the provider is
        /// completed. The default implementation of this method does nothing. It can be overriden
        /// by a derived class such that the provider can be further configured before it is
        /// completed
        /// </summary>
        /// <param name="descriptor">
        /// The descriptor that can be used to configure the provider
        /// </param>
        protected virtual void Configure(ISortProviderDescriptor<TContext> descriptor) { }

        /// <summary>
        /// Creates the executor that is attached to the middleware pipeline of the field
        /// </summary>
        /// <param name="argumentName">
        /// The argument name specified in the <see cref="SortConvention"/>
        /// </param>
        /// <typeparam name="TEntityType">The runtime type of the entity</typeparam>
        /// <returns>A middleware</returns>
        public abstract FieldMiddleware CreateExecutor<TEntityType>(NameString argumentName);

        /// <summary>
        /// Is called on each field that sorting is applied to. This method can be used to
        /// customize a field.
        /// </summary>
        /// <param name="argumentName">
        /// The argument name specified in the <see cref="SortConvention"/>
        /// </param>
        /// <param name="descriptor">The descriptor of the field</param>
        public virtual void ConfigureField(
            NameString argumentName,
            IObjectFieldDescriptor descriptor)
        {
        }
    }
}
