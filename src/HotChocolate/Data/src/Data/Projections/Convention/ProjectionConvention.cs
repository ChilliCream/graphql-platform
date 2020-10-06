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
    public abstract class ProjectionConvention
        : Convention<ProjectionConventionDefinition>,
          IProjectionConvention
    {
        private Action<IProjectionConventionDescriptor>? _configure;

        private readonly IList<IProjectionFieldHandler> _fieldHandlers =
            new List<IProjectionFieldHandler>();

        private readonly IList<IProjectionFieldInterceptor> _fieldInterceptors =
            new List<IProjectionFieldInterceptor>();

        private readonly IList<IProjectionOptimizer> _optimizer = new List<IProjectionOptimizer>();

        protected ProjectionConvention()
        {
            _configure = Configure;
        }

        public ProjectionConvention(Action<IProjectionConventionDescriptor> configure)
        {
            _configure = configure ??
                throw new ArgumentNullException(nameof(configure));
        }

        protected override ProjectionConventionDefinition CreateDefinition(
            IConventionContext context)
        {
            if (_configure is null)
            {
                throw new InvalidOperationException(ProjectionConvention_NoConfigurationSpecified);
            }

            var descriptor = ProjectionConventionDescriptor.New(
                context.DescriptorContext,
                context.Scope);

            _configure(descriptor);
            _configure = null;

            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IProjectionConventionDescriptor descriptor)
        {
        }

        protected override void OnComplete(
            IConventionContext context,
            ProjectionConventionDefinition definition)
        {
            if (definition.Handlers.Count == 0)
            {
                throw ProjectionConvention_NoHandlersConfigured(this);
            }

            IServiceProvider services = new DictionaryServiceProvider(
                    (typeof(IConventionContext), context),
                    (typeof(IProjectionConvention), context.Convention),
                    (typeof(IDescriptorContext), context.DescriptorContext),
                    (typeof(ITypeInspector), context.DescriptorContext.TypeInspector))
                .Include(context.Services);

            foreach ((Type Type, IProjectionFieldHandler? Instance) handler in definition.Handlers)
            {
                switch (handler.Instance)
                {
                    case null when services.TryGetOrCreateService(
                        handler.Type,
                        out IProjectionFieldHandler? service):
                        _fieldHandlers.Add(service);
                        break;
                    case null:
                        context.ReportError(
                            ProjectionConvention_UnableToCreateFieldHandler(this, handler.Type));
                        break;
                    default:
                        _fieldHandlers.Add(handler.Instance);
                        break;
                }
            }

            foreach ((Type Type, IProjectionFieldInterceptor? Instance) handler in
                definition.Interceptors)
            {
                switch (handler.Instance)
                {
                    case null when services.TryGetOrCreateService(
                        handler.Type,
                        out IProjectionFieldInterceptor? service):
                        _fieldInterceptors.Add(service);
                        break;
                    case null:
                        context.ReportError(
                            ProjectionConvention_UnableToCreateFieldHandler(this, handler.Type));
                        break;
                    default:
                        _fieldInterceptors.Add(handler.Instance);
                        break;
                }
            }

            foreach ((Type Type, IProjectionOptimizer? Instance) handler in
                definition.Optimizers)
            {
                switch (handler.Instance)
                {
                    case null when services.TryGetOrCreateService(
                        handler.Type,
                        out IProjectionOptimizer? service):
                        _optimizer.Add(service);
                        break;
                    case null:
                        context.ReportError(
                            ProjectionConvention_UnableToCreateFieldHandler(this, handler.Type));
                        break;
                    default:
                        _optimizer.Add(handler.Instance);
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

        public abstract FieldMiddleware CreateExecutor<TEntityType>();
    }
}
