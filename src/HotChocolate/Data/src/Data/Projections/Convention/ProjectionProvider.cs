using System;
using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using static HotChocolate.Data.DataResources;
using static HotChocolate.Data.ErrorHelper;
using static HotChocolate.Data.ThrowHelper;

namespace HotChocolate.Data.Projections
{
    /// <summary>
    /// A <see cref="ProjectionProvider"/> translates a incoming query to another
    /// object structure at runtime
    /// </summary>
    public abstract class ProjectionProvider
        : Convention<ProjectionProviderDefinition>
        , IProjectionProvider
        , IProjectionProviderConvention
    {
        private Action<IProjectionProviderDescriptor>? _configure;

        private readonly IList<IProjectionFieldHandler> _fieldHandlers =
            new List<IProjectionFieldHandler>();

        private readonly IList<IProjectionFieldInterceptor> _fieldInterceptors =
            new List<IProjectionFieldInterceptor>();

        private readonly IList<IProjectionOptimizer> _optimizer = new List<IProjectionOptimizer>();

        public const string ProjectionContextIdentifier = "ProjectionMiddleware";

        protected ProjectionProvider()
        {
            _configure = Configure;
        }

        public ProjectionProvider(Action<IProjectionProviderDescriptor> configure)
        {
            _configure = configure ??
                throw new ArgumentNullException(nameof(configure));
        }

        internal new ProjectionProviderDefinition? Definition => base.Definition;

        /// <inheritdoc />
        protected override ProjectionProviderDefinition CreateDefinition(IConventionContext context)
        {
            if (_configure is null)
            {
                throw new InvalidOperationException(ProjectionConvention_NoConfigurationSpecified);
            }

            var descriptor =
                ProjectionProviderDescriptor.New(context.DescriptorContext, context.Scope);

            _configure(descriptor);
            _configure = null;

            return descriptor.CreateDefinition();
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
        protected virtual void Configure(IProjectionProviderDescriptor descriptor)
        {
        }

        void IProjectionProviderConvention.Complete(IConventionContext context)
        {
            Complete(context);
        }

        protected override void Complete(IConventionContext context)
        {
            if (Definition.Handlers.Count == 0)
            {
                throw ProjectionProvider_NoHandlersConfigured(this);
            }

            IServiceProvider services = new DictionaryServiceProvider(
                    (typeof(IConventionContext), context),
                    (typeof(IDescriptorContext), context.DescriptorContext),
                    (typeof(ITypeInspector), context.DescriptorContext.TypeInspector))
                .Include(context.Services);

            foreach ((Type type, IProjectionFieldHandler? instance) in Definition.Handlers)
            {
                switch (instance)
                {
                    case null when services.TryGetOrCreateService(
                        type,
                        out IProjectionFieldHandler? service):
                        _fieldHandlers.Add(service);
                        break;

                    case null:
                        throw new SchemaException(
                            ProjectionConvention_UnableToCreateFieldHandler(this, type));

                    default:
                        _fieldHandlers.Add(instance);
                        break;
                }
            }

            foreach ((var type, IProjectionFieldInterceptor? instance) in Definition.Interceptors)
            {
                switch (instance)
                {
                    case null when services.TryGetOrCreateService(
                        type,
                        out IProjectionFieldInterceptor? service):
                        _fieldInterceptors.Add(service);
                        break;

                    case null:
                        throw new SchemaException(
                            ProjectionConvention_UnableToCreateFieldHandler(this, type));

                    default:
                        _fieldInterceptors.Add(instance);
                        break;
                }
            }

            foreach ((var type, IProjectionOptimizer? instance) in Definition.Optimizers)
            {
                switch (instance)
                {
                    case null when services.TryGetOrCreateService(
                        type,
                        out IProjectionOptimizer? service):
                        _optimizer.Add(service);
                        break;

                    case null:
                        throw new SchemaException(
                            ProjectionConvention_UnableToCreateFieldHandler(this, type));

                    default:
                        _optimizer.Add(instance);
                        break;
                }
            }
        }

        public Selection RewriteSelection(
            SelectionOptimizerContext context,
            Selection selection)
        {
            for (var i = 0; i < _optimizer.Count; i++)
            {
                if (_optimizer[i].CanHandle(selection))
                {
                    selection = _optimizer[i].RewriteSelection(context, selection);
                }
            }

            for (var i = 0; i < _fieldHandlers.Count; i++)
            {
                if (_fieldHandlers[i].CanHandle(selection))
                {
                    IProjectionFieldHandler fieldHandler = _fieldHandlers[i];

                    for (var m = 0; m < _fieldInterceptors.Count; m++)
                    {
                        if (_fieldInterceptors[m].CanHandle(selection))
                        {
                            fieldHandler = fieldHandler.Wrap(_fieldInterceptors[m]);
                        }
                    }

                    return ProjectionSelection.From(selection, fieldHandler);
                }
            }

            return selection;
        }

        void IProjectionProviderConvention.Initialize(IConventionContext context)
        {
            base.Initialize(context);
        }

        public new void Initialize(IConventionContext context)
        {
            base.Initialize(context);
        }

        /// <summary>
        /// Creates the executor that is attached to the middleware pipeline of the field
        /// </summary>
        /// <typeparam name="TEntityType">The runtime type of the entity</typeparam>
        /// <returns>A middleware</returns>
        public abstract FieldMiddleware CreateExecutor<TEntityType>();
    }
}
