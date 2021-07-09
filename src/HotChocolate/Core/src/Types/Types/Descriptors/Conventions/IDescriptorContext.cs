using System;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    /// <summary>
    /// The descriptor context is passed around during the schema creation and
    /// allows access to conventions and context data.
    /// </summary>
    public interface IDescriptorContext : IHasContextData, IDisposable
    {
        event EventHandler<SchemaCompletedEventArgs> SchemaCompleted;

        /// <summary>
        /// Gets the schema options.
        /// </summary>
        IReadOnlySchemaOptions Options { get; }

        /// <summary>
        /// Gets the schema services.
        /// </summary>
        IServiceProvider Services { get; }

        /// <summary>
        /// Gets the naming conventions.
        /// </summary>
        INamingConventions Naming { get; }

        /// <summary>
        /// Gets the type inspector.
        /// </summary>
        ITypeInspector TypeInspector { get; }

        /// <summary>
        /// Gets the schema interceptor.
        /// </summary>
        SchemaInterceptor SchemaInterceptor { get; }

        /// <summary>
        /// Gets the type interceptor.
        /// </summary>
        TypeInterceptor TypeInterceptor { get; }

        /// <summary>
        /// Gets the resolver compiler.
        /// </summary>
        IResolverCompiler ResolverCompiler { get; }

        /// <summary>
        /// Gets a custom convention.
        /// </summary>
        /// <param name="defaultConvention">The default contention.</param>
        /// <param name="scope">An optional scope for this convention.</param>
        /// <typeparam name="T">The type of the convention.</typeparam>
        /// <returns>
        /// Returns the convention.
        /// </returns>
        T GetConventionOrDefault<T>(Func<T> defaultConvention, string? scope = null)
            where T : class, IConvention;
    }
}
