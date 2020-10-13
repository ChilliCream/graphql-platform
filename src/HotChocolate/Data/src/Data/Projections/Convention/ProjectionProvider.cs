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
    /// The filter convention provides defaults for inferring filters.
    /// </summary>
    public abstract class ProjectionProvider
        : Convention<ProjectionProviderDefinition>,
          IProjectionProvider,
          IProjectionProviderConvention
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

        protected override ProjectionProviderDefinition CreateDefinition(
            IConventionContext context)
        {
            if (_configure is null)
            {
                throw new InvalidOperationException(ProjectionConvention_NoConfigurationSpecified);
            }

            var descriptor = ProjectionProviderDescriptor.New(
                context.DescriptorContext,
                context.Scope);

            _configure(descriptor);
            _configure = null;

            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IProjectionProviderDescriptor descriptor)
        {
        }

        protected override void OnComplete(
            IConventionContext context,
            ProjectionProviderDefinition definition)
        {
            if (definition.Handlers.Count == 0)
            {
                throw ProjectionProvider_NoHandlersConfigured(this);
            }

            IServiceProvider services = new DictionaryServiceProvider(
                    (typeof(IConventionContext), context),
                    (typeof(IProjectionProvider), context.Convention),
                    (typeof(IDescriptorContext), context.DescriptorContext),
                    (typeof(ITypeInspector), context.DescriptorContext.TypeInspector))
                .Include(context.Services);

            foreach ((Type type, IProjectionFieldHandler? instance) in definition.Handlers)
            {
                switch (instance)
                {
                    case null when services.TryGetOrCreateService(
                        type,
                        out IProjectionFieldHandler? service):
                        _fieldHandlers.Add(service);
                        break;
                    case null:
                        context.ReportError(
                            ProjectionConvention_UnableToCreateFieldHandler(this, type));
                        break;
                    default:
                        _fieldHandlers.Add(instance);
                        break;
                }
            }

            foreach ((var type, IProjectionFieldInterceptor? instance) in definition.Interceptors)
            {
                switch (instance)
                {
                    case null when services.TryGetOrCreateService(
                        type,
                        out IProjectionFieldInterceptor? service):
                        _fieldInterceptors.Add(service);
                        break;
                    case null:
                        context.ReportError(
                            ProjectionConvention_UnableToCreateFieldHandler(this, type));
                        break;
                    default:
                        _fieldInterceptors.Add(instance);
                        break;
                }
            }

            foreach ((var type, IProjectionOptimizer? instance) in definition.Optimizers)
            {
                switch (instance)
                {
                    case null when services.TryGetOrCreateService(
                        type,
                        out IProjectionOptimizer? service):
                        _optimizer.Add(service);
                        break;
                    case null:
                        context.ReportError(
                            ProjectionConvention_UnableToCreateFieldHandler(this, type));
                        break;
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

                    return ProjectionSelection.From(
                        selection,
                        fieldHandler);
                }
            }

            return selection;
        }

        public new void Initialize(IConventionContext context)
        {
            base.Initialize(context);
        }

        /// <inheritdoc />
        public abstract FieldMiddleware CreateExecutor<TEntityType>();
    }
}
