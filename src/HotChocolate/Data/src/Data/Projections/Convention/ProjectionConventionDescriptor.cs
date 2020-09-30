using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Projections
{
    public class ProjectionConventionDescriptor
        : IProjectionConventionDescriptor
    {
        protected ProjectionConventionDescriptor(IDescriptorContext context, string? scope)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Definition.Scope = scope;
        }

        protected IDescriptorContext Context { get; }

        protected ProjectionConventionDefinition Definition { get; } =
            new ProjectionConventionDefinition();

        public ProjectionConventionDefinition CreateDefinition()
        {
            return Definition;
        }

        public IProjectionConventionDescriptor RegisterFieldHandler<THandler>()
            where THandler : IProjectionFieldHandler
        {
            Definition.Handlers.Add((typeof(THandler), null));
            return this;
        }

        public IProjectionConventionDescriptor RegisterFieldHandler<THandler>(THandler handler)
            where THandler : IProjectionFieldHandler
        {
            Definition.Handlers.Add((typeof(THandler), handler));
            return this;
        }

        /// <summary>
        /// Creates a new descriptor for <see cref="ProjectionConvention"/>
        /// </summary>
        /// <param name="context">The descriptor context.</param>
        /// <param name="scope">The scope</param>
        public static ProjectionConventionDescriptor New(
            IDescriptorContext context,
            string? scope) =>
            new ProjectionConventionDescriptor(context, scope);
    }
}
