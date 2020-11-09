using System;
using System.Collections.Generic;
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

        protected override void Complete(IConventionContext context)
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
                IReadOnlyList<IProjectionProviderExtension> extensions =
                    CollectExtensions(context.Services, Definition);
                init.Initialize(context);
                MergeExtensions(context, init, extensions);
                init.Complete(context);
            }
        }

        public FieldMiddleware CreateExecutor<TEntityType>() =>
            _provider.CreateExecutor<TEntityType>();

        public ISelectionOptimizer CreateOptimizer() =>
            new ProjectionOptimizer(_provider);

        private static IReadOnlyList<IProjectionProviderExtension> CollectExtensions(
            IServiceProvider serviceProvider,
            ProjectionConventionDefinition definition)
        {
            List<IProjectionProviderExtension> extensions = new List<IProjectionProviderExtension>();
            extensions.AddRange(definition.ProviderExtensions);
            foreach (var extensionType in definition.ProviderExtensionsTypes)
            {
                if (serviceProvider.TryGetOrCreateService<IProjectionProviderExtension>(
                    extensionType,
                    out var createdExtension))
                {
                    extensions.Add(createdExtension);
                }
            }

            return extensions;
        }

        private static void MergeExtensions(
            IConventionContext context,
            IProjectionProviderConvention provider,
            IReadOnlyList<IProjectionProviderExtension> extensions)
        {
            if (provider is Convention providerConvention)
            {
                for (var m = 0; m < extensions.Count; m++)
                {
                    if (extensions[m] is IProjectionProviderConvention extensionConvention)
                    {
                        extensionConvention.Initialize(context);
                        extensions[m].Merge(context, providerConvention);
                        extensionConvention.Complete(context);
                    }
                }
            }
        }
    }
}

