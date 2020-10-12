using System;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Projections
{
    public class ProjectionConvention
        : Convention<ProjectionConventionDefinition>,
          IProjectionConvention
    {
        private Action<IProjectionConventionDescriptor>? _configure;
        private IProjectionProvider _provider;

        public const string IsProjectedKey = nameof(IsProjectedKey);
        public const string AlwaysProjectedFieldsKey = nameof(AlwaysProjectedFieldsKey);

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
                throw new InvalidOperationException(
                    DataResources.ProjectionConvention_NoConfigurationSpecified);
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
            if (definition.Provider is null)
            {
                throw ThrowHelper.ProjectionConvention_NoProviderFound(GetType(), definition.Scope);
            }

            if (definition.ProviderInstance is null)
            {
                _provider =
                    context.Services.GetOrCreateService<IProjectionProvider>(definition.Provider) ??
                    throw ThrowHelper.ProjectionConvention_NoProviderFound(
                        GetType(),
                        definition.Scope);
            }
            else
            {
                _provider = definition.ProviderInstance;
            }

            if (_provider is IProjectionProviderConvention init)
            {
                init.Initialize(context);
            }
        }

        public FieldMiddleware CreateExecutor<TEntityType>() =>
            _provider.CreateExecutor<TEntityType>();

        public Selection RewriteSelection(
            SelectionOptimizerContext context,
            Selection selection) =>
            _provider.RewriteSelection(context, selection);
    }
}
