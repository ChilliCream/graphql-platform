using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Mocha.Analyzers;

/// <summary>
/// Represents a cache of commonly used Mocha mediator type symbols resolved from a
/// <see cref="Compilation"/>. Symbols are resolved lazily on first access.
/// A single instance is shared across all syntax nodes for a given compilation via
/// <see cref="GetOrCreate"/>.
/// </summary>
public sealed class KnownTypeSymbols
{
    private static readonly ConditionalWeakTable<Compilation, KnownTypeSymbols> s_cache = new();

    private readonly Compilation _compilation;

    private Resolved<INamedTypeSymbol?>? _commandHandlerVoid;
    private Resolved<INamedTypeSymbol?>? _commandHandlerResponse;
    private Resolved<INamedTypeSymbol?>? _queryHandler;
    private Resolved<INamedTypeSymbol?>? _notificationHandler;
    private Resolved<INamedTypeSymbol?>? _commandVoid;
    private Resolved<INamedTypeSymbol?>? _commandOfT;
    private Resolved<INamedTypeSymbol?>? _queryOfT;

    private KnownTypeSymbols(Compilation compilation)
    {
        _compilation = compilation;
    }

    /// <summary>
    /// Gets or creates a <see cref="KnownTypeSymbols"/> instance for the specified compilation.
    /// The instance is cached and reused for the lifetime of the <paramref name="compilation"/> object.
    /// </summary>
    public static KnownTypeSymbols GetOrCreate(Compilation compilation)
        => s_cache.GetValue(compilation, static c => new KnownTypeSymbols(c));

    /// <summary>
    /// Gets the symbol for the <c>ICommandHandler&lt;TCommand&gt;</c> interface (void return).
    /// </summary>
    public INamedTypeSymbol? ICommandHandlerVoid
        => Resolve(SyntaxConstants.ICommandHandlerVoid, ref _commandHandlerVoid);

    /// <summary>
    /// Gets the symbol for the <c>ICommandHandler&lt;TCommand, TResponse&gt;</c> interface.
    /// </summary>
    public INamedTypeSymbol? ICommandHandlerResponse
        => Resolve(SyntaxConstants.ICommandHandlerResponse, ref _commandHandlerResponse);

    /// <summary>
    /// Gets the symbol for the <c>IQueryHandler&lt;TQuery, TResponse&gt;</c> interface.
    /// </summary>
    public INamedTypeSymbol? IQueryHandler
        => Resolve(SyntaxConstants.IQueryHandler, ref _queryHandler);

    /// <summary>
    /// Gets the symbol for the <c>INotificationHandler&lt;TNotification&gt;</c> interface.
    /// </summary>
    public INamedTypeSymbol? INotificationHandler
        => Resolve(SyntaxConstants.INotificationHandler, ref _notificationHandler);

    /// <summary>
    /// Gets the symbol for the <c>ICommand</c> marker interface (void return).
    /// </summary>
    public INamedTypeSymbol? ICommandVoid
        => Resolve(SyntaxConstants.ICommand, ref _commandVoid);

    /// <summary>
    /// Gets the symbol for the <c>ICommand&lt;TResponse&gt;</c> interface.
    /// </summary>
    public INamedTypeSymbol? ICommandOfT
        => Resolve(SyntaxConstants.ICommandOfT, ref _commandOfT);

    /// <summary>
    /// Gets the symbol for the <c>IQuery&lt;TResponse&gt;</c> interface.
    /// </summary>
    public INamedTypeSymbol? IQueryOfT
        => Resolve(SyntaxConstants.IQueryOfT, ref _queryOfT);

    private INamedTypeSymbol? Resolve(string metadataName, ref Resolved<INamedTypeSymbol?>? field)
    {
        var snapshot = Interlocked.CompareExchange(ref field, null, null);
        if (snapshot is not null)
        {
            return snapshot.Value;
        }

        var resolved = new Resolved<INamedTypeSymbol?>(_compilation.GetTypeByMetadataName(metadataName));
        var existing = Interlocked.CompareExchange(ref field, resolved, null);
        return (existing ?? resolved).Value;
    }

    private sealed class Resolved<T>(T value)
    {
        public readonly T Value = value;
    }
}
