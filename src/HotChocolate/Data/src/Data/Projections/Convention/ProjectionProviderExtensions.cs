using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Projections
{
    public abstract class ProjectionProviderExtensions
        : ConventionExtension<ProjectionProviderDefinition>,
          IProjectionProviderExtension,
          IProjectionProviderConvention
    {
        private Action<IProjectionProviderDescriptor>? _configure;

        protected ProjectionProviderExtensions()
        {
            _configure = Configure;
        }

        public ProjectionProviderExtensions(Action<IProjectionProviderDescriptor> configure)
        {
            _configure = configure ??
                throw new ArgumentNullException(nameof(configure));
        }

        void IProjectionProviderConvention.Initialize(IConventionContext context)
        {
            base.Initialize(context);
        }

        void IProjectionProviderConvention.OnComplete(IConventionContext context)
        {
            OnComplete(context);
        }

        protected override ProjectionProviderDefinition CreateDefinition(IConventionContext context)
        {
            if (_configure is null)
            {
                throw new InvalidOperationException(
                    DataResources.ProjectionProvider_NoConfigurationSpecified);
            }

            var descriptor = ProjectionProviderDescriptor.New(
                context.DescriptorContext,
                context.Scope);

            _configure(descriptor);
            _configure = null;

            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IProjectionProviderDescriptor descriptor) { }

        public override void Merge(IConventionContext context, Convention convention)
        {
            if (Definition is not null &&
                convention is ProjectionProvider conv &&
                conv.Definition is {} target)
            {
                // Provider extensions should be applied by default before the default handlers, as
                // the interceptor picks up the first handler. A provider extension should adds more
                // specific handlers than the default providers
                for (var i = Definition.Handlers.Count - 1; i >= 0; i--)
                {
                    target.Handlers.Insert(0, Definition.Handlers[i]);
                }
            }
        }
    }
}
