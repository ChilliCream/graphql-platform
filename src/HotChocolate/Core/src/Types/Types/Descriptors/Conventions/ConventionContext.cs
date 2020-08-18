using System;
using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    internal sealed class ConventionContext : IConventionContext
    {
        public ConventionContext(
            IConvention convention,
            string? scope,
            IServiceProvider services,
            IDescriptorContext descriptorContext)
        {
            Convention = convention;
            Scope = scope;
            Services = services;
            DescriptorContext = descriptorContext;
        }

        /// <inheritdoc />
        public IConvention Convention { get; }

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
    }
}
