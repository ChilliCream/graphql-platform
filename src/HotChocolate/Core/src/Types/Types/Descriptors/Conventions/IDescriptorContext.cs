using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Relay;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors;

/// <summary>
/// The descriptor context is passed around during the schema creation and
/// allows access to conventions and context data.
/// </summary>
public interface IDescriptorContext : IHasContextData, IDisposable
{
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
    /// Gets the type interceptor.
    /// </summary>
    TypeInterceptor TypeInterceptor { get; }

    /// <summary>
    /// Gets the resolver compiler.
    /// </summary>
    IResolverCompiler ResolverCompiler { get; }

    /// <summary>
    /// Gets the type converter.
    /// </summary>
    ITypeConverter TypeConverter { get; }

    /// <summary>
    /// Gets the input parser.
    /// </summary>
    InputParser InputParser { get; }

    /// <summary>
    /// Gets the input formatter.
    /// </summary>
    InputFormatter InputFormatter { get; }

    /// <summary>
    /// Gets the descriptor currently in path.
    /// </summary>
    IList<IDescriptor> Descriptors { get; }

    /// <summary>
    /// Gets an accessor to get access to the current node id serializer.
    /// </summary>
    INodeIdSerializerAccessor NodeIdSerializerAccessor { get; }

    /// <summary>
    /// Gets the parameter binding resolver.
    /// </summary>
    IParameterBindingResolver ParameterBindingResolver { get; }

    /// <summary>
    /// Gets the registered type discovery handlers.
    /// </summary>
    ReadOnlySpan<TypeDiscoveryHandler> GetTypeDiscoveryHandlers();

    /// <summary>
    /// Tries to resolve a schema building directive for the
    /// specified <paramref name="directiveNode"/>.
    /// </summary>
    bool TryGetSchemaDirective(
        DirectiveNode directiveNode,
        [NotNullWhen(true)] out ISchemaDirective? directive);

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

    /// <summary>
    /// Allows to subscribe to schema completed events.
    /// </summary>
    void OnSchemaCreated(Action<ISchema> callback);
}
