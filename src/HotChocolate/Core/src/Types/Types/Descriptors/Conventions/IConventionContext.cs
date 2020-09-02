using System;
using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    /// <summary>
    /// The convention context is available during the convention initialization process.
    /// </summary>
    public interface IConventionContext : IHasScope
    {
        /// <summary>
        /// The convention that is being initialized.
        /// </summary>
        IConvention Convention { get; }

        /// <summary>
        /// The schema level services.
        /// </summary>
        IServiceProvider Services { get; }

        /// <summary>
        /// The schema builder context data that can be used for extensions
        /// to pass state along the initialization process.
        /// This property can also be reached through <see cref="IDescriptorContext.ContextData" />.
        /// </summary>
        IDictionary<string, object?> ContextData { get; }

        /// <summary>
        /// The descriptor context that is passed through the initialization process.
        /// </summary>
        IDescriptorContext DescriptorContext { get; }

        /// <summary>
        /// Report a schema initialization error.
        /// </summary>
        /// <param name="error">
        /// The error that occurred during initialization.
        /// </param>
        void ReportError(ISchemaError error);
    }
}
