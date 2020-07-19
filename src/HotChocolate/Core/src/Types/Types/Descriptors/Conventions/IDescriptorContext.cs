using System;
using System.Collections.Generic;
using HotChocolate.Configuration;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    /// <summary>
    /// The descriptor context is passed around during the schema creation and 
    /// allows access to conventions and context data.
    /// </summary>
    public interface IDescriptorContext
    {
        /// <summary>
        /// Gets the schema options.
        /// </summary>
        IReadOnlySchemaOptions Options { get; }

        /// <summary>
        /// Gets the naming conventions.
        /// </summary>
        INamingConventions Naming { get; }

        /// <summary>
        /// Gets the type inspector.
        /// </summary>
        ITypeInspector Inspector { get; }

        /// <summary>
        /// Gets the context for the schema creation process. 
        /// These context data are passed along into every type and 
        /// will be cleared at the end of the schema creation process.
        /// </summary>
        IDictionary<string, object?> ContextData { get; }

        /// <summary>
        /// Gets a custom convention.
        /// </summary>
        /// <param name="defaultConvention">The default contention.</param>
        /// <typeparam name="T">The type of the convention.</typeparam>
        /// <returns>
        /// Returns the convention.
        /// </returns>
        T GetConventionOrDefault<T>(string? scope, Func<T> defaultConvention)
            where T : class, IConvention;
    }
}
