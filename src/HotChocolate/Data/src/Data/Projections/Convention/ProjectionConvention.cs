using System;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using static HotChocolate.Data.ThrowHelper;

namespace HotChocolate.Data.Projections
{
    public class ProjectionConvention
        : Convention<ProjectionConventionDefinition>
        , IProjectionConvention
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

        internal new ProjectionConventionDefinition? Definition => base.Definition;

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

        protected override void OnComplete(IConventionContext context)
        {
            if (Definition.Provider is null)
            {
                throw ProjectionConvention_NoProviderFound(GetType(), Definition.Scope);
            }

            if (Definition.ProviderInstance is null)
            {
                _provider =
                    context.Services.GetOrCreateService<IProjectionProvider>(Definition.Provider) ??
                    throw ProjectionConvention_NoProviderFound(GetType(), Definition.Scope);
            }
            else
            {
                _provider = Definition.ProviderInstance;
            }

            if (_provider is IProjectionProviderConvention init)
            {
                init.Initialize(context);
                init.OnComplete(context);
            }
        }

        public FieldMiddleware CreateExecutor<TEntityType>() =>
            _provider.CreateExecutor<TEntityType>();

        public ISelectionOptimizer CreateOptimizer() =>
            new ProjectionOptimizer(_provider);
    }
}

