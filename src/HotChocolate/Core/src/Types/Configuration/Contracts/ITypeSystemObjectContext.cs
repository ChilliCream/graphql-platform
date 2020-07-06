using System;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Configuration
{
    /// <summary>
    /// The type system context is available during the type system initialization process.
    /// </summary>
    public interface ITypeSystemObjectContext
    {
        /// <summary>
        /// The type system object that is being initialized.
        /// </summary>
        ITypeSystemObject Type { get; }

        /// <summary>
        /// Defines if <see cref="Type" /> is a type like the object type or interface type.
        /// </summary>
        bool IsType { get; }

        /// <summary>
        /// Defines if <see cref="Type" /> is an introspection type.
        /// </summary>
        /// <value></value>
        bool IsIntrospectionType { get; }

        /// <summary>
        /// Defines if <see cref="Type" /> is a directive.
        /// </summary>
        bool IsDirective { get; }

        /// <summary>
        /// Defines if <see cref="Type" /> is a schema.
        /// </summary>
        bool IsSchema { get; }

        /// <summary>
        /// The schema level services.
        /// </summary>
        IServiceProvider Services { get; }

        /// <summary>
        /// The schema builder context data that can be used for extensions 
        /// to pass state along the initialization process.
        /// This property can also be reached through <see cref="DescriptorContext.ContextData" />.
        /// </summary>
        IDictionary<string, object?> ContextData { get; }

        /// <summary>
        /// The descriptor context that is passed through the initialization process.
        /// </summary>
        IDescriptorContext DescriptorContext { get; }

        /// <summary>
        /// The type initialization interceptor that allows to intercept 
        /// objects that er being initialized.
        /// </summary>
        ITypeInitializationInterceptor Interceptor { get; }

        /// <summary>
        /// Report a schema initialization error.
        /// </summary>
        /// <param name="error">
        /// The error that occurred during initialization.
        /// </param>
        void ReportError(ISchemaError error);
    }
}
