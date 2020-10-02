using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using static HotChocolate.Data.DataResources;
using static HotChocolate.Data.ErrorHelper;
using static HotChocolate.Data.ThrowHelper;

namespace HotChocolate.Data.Projections
{
    public class ProjectionSelection
        : Selection,
          IProjectionSelection
    {
        public ProjectionSelection(
            IProjectionFieldHandler handler,
            IObjectType declaringType,
            IObjectField field,
            FieldNode selection,
            FieldDelegate resolverPipeline,
            NameString? responseName = null,
            IReadOnlyDictionary<NameString, ArgumentValue>? arguments = null,
            SelectionIncludeCondition? includeCondition = null,
            bool internalSelection = false) : base(
            declaringType,
            field,
            selection,
            resolverPipeline,
            responseName,
            arguments,
            includeCondition,
            internalSelection)
        {
            Handler = handler;
        }

        public IProjectionFieldHandler Handler { get; }

        public static ProjectionSelection From(
            Selection selection,
            IProjectionFieldHandler handler) =>
            new ProjectionSelection(
                handler,
                selection.DeclaringType,
                selection.Field,
                selection.SyntaxNode,
                selection.ResolverPipeline,
                selection.ResponseName,
                selection.Arguments,
                selection.IncludeConditions?.FirstOrDefault(),
                selection.IsInternal);
    }

    /// <summary>
    /// The filter convention provides defaults for inferring filters.
    /// </summary>
    public abstract class ProjectionConvention
        : Convention<ProjectionConventionDefinition>,
          IProjectionConvention
    {
        private Action<IProjectionConventionDescriptor>? _configure;

        public IList<IProjectionFieldHandler> _fieldHandlers { get; } =
            new List<IProjectionFieldHandler>();

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
        }

        public Selection RewriteSelection(Selection selection)
        {
            for (var i = 0; i < _fieldHandlers.Count; i++)
            {
                if (_fieldHandlers[i].CanHandle(selection))
                {
                    selection = _fieldHandlers[i].RewriteSelection(selection);
                    return ProjectionSelection.From(selection, _fieldHandlers[i]);
                }
            }

            return selection;
        }

        public abstract FieldMiddleware CreateExecutor<TEntityType>();
    }
}
