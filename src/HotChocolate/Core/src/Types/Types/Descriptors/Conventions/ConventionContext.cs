using System;
using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    internal sealed class ConventionContext : IConventionContext
    {
        public ConventionContext(
            string? scope,
            IServiceProvider services,
            IDescriptorContext descriptorContext)
        {
            Scope = scope;
            Services = services;
            DescriptorContext = descriptorContext;
        }

        /// <inheritdoc />
        public string? Scope { get; }

        /// <inheritdoc />
        public IServiceProvider Services { get; }

        /// <inheritdoc />
        public IDictionary<string, object?> ContextData => DescriptorContext.ContextData;

        /// <inheritdoc />
        public IDescriptorContext DescriptorContext { get; }

        /// <inheritdoc />
        public void ReportError(ISchemaError error)
        {
            throw new NotImplementedException();
        }

        public static ConventionContext Create(
            string? scope,
            IServiceProvider services,
            IDescriptorContext descriptorContext) =>
            new ConventionContext(
                scope,
                services,
                descriptorContext);
    }
}
