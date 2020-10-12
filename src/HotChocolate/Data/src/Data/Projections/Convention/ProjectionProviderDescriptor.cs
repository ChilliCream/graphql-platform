using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Projections
{
    public class ProjectionProviderDescriptor
        : IProjectionProviderDescriptor
    {
        protected ProjectionProviderDescriptor(IDescriptorContext context, string? scope)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Definition.Scope = scope;
        }

        protected IDescriptorContext Context { get; }

        protected ProjectionProviderDefinition Definition { get; } =
            new ProjectionProviderDefinition();

        public ProjectionProviderDefinition CreateDefinition()
        {
            return Definition;
        }

        public IProjectionProviderDescriptor RegisterFieldHandler<THandler>()
            where THandler : IProjectionFieldHandler
        {
            Definition.Handlers.Add((typeof(THandler), null));
            return this;
        }

        public IProjectionProviderDescriptor RegisterFieldHandler<THandler>(THandler handler)
            where THandler : IProjectionFieldHandler
        {
            Definition.Handlers.Add((typeof(THandler), handler));
            return this;
        }

        public IProjectionProviderDescriptor RegisterFieldInterceptor<THandler>()
            where THandler : IProjectionFieldInterceptor
        {
            Definition.Interceptors.Add((typeof(THandler), null));
            return this;
        }

        public IProjectionProviderDescriptor RegisterFieldInterceptor<THandler>(THandler handler)
            where THandler : IProjectionFieldInterceptor
        {
            Definition.Interceptors.Add((typeof(THandler), handler));
            return this;
        }

        public IProjectionProviderDescriptor RegisterOptimizer<THandler>()
            where THandler : IProjectionOptimizer
        {
            Definition.Optimizers.Add((typeof(THandler), null));
            return this;
        }

        public IProjectionProviderDescriptor RegisterOptimizer<THandler>(THandler handler)
            where THandler : IProjectionOptimizer
        {
            Definition.Optimizers.Add((typeof(THandler), handler));
            return this;
        }

        /// <summary>
        /// Creates a new descriptor for <see cref="ProjectionProvider"/>
        /// </summary>
        /// <param name="context">The descriptor context.</param>
        /// <param name="scope">The scope</param>
        public static ProjectionProviderDescriptor New(
            IDescriptorContext context,
            string? scope) =>
            new ProjectionProviderDescriptor(context, scope);
    }
}
