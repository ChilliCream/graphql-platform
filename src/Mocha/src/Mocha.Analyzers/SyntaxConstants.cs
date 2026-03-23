namespace Mocha.Analyzers;

/// <summary>
/// Provides CLR metadata names for Mocha mediator types used to resolve
/// <see cref="Microsoft.CodeAnalysis.INamedTypeSymbol"/> instances from a compilation.
/// </summary>
public static class SyntaxConstants
{
    /// <summary>
    /// Gets the metadata name for the <c>ICommandHandler&lt;TCommand&gt;</c> interface (void return).
    /// </summary>
    public const string ICommandHandlerVoid = "Mocha.Mediator.ICommandHandler`1";

    /// <summary>
    /// Gets the metadata name for the <c>ICommandHandler&lt;TCommand, TResponse&gt;</c> interface.
    /// </summary>
    public const string ICommandHandlerResponse = "Mocha.Mediator.ICommandHandler`2";

    /// <summary>
    /// Gets the metadata name for the <c>IQueryHandler&lt;TQuery, TResponse&gt;</c> interface.
    /// </summary>
    public const string IQueryHandler = "Mocha.Mediator.IQueryHandler`2";

    /// <summary>
    /// Gets the metadata name for the <c>INotificationHandler&lt;TNotification&gt;</c> interface.
    /// </summary>
    public const string INotificationHandler = "Mocha.Mediator.INotificationHandler`1";

    /// <summary>
    /// Gets the metadata name for the <c>ICommand</c> marker interface (void return).
    /// </summary>
    public const string ICommand = "Mocha.Mediator.ICommand";

    /// <summary>
    /// Gets the metadata name for the <c>ICommand&lt;TResponse&gt;</c> interface.
    /// </summary>
    public const string ICommandOfT = "Mocha.Mediator.ICommand`1";

    /// <summary>
    /// Gets the metadata name for the <c>IQuery&lt;TResponse&gt;</c> interface.
    /// </summary>
    public const string IQueryOfT = "Mocha.Mediator.IQuery`1";

    /// <summary>
    /// Gets the metadata name for the <c>INotification</c> marker interface.
    /// </summary>
    public const string INotificationMarker = "Mocha.Mediator.INotification";

    /// <summary>
    /// Gets the metadata name for the <c>MediatorModuleAttribute</c> class.
    /// </summary>
    public const string MediatorModuleAttribute = "Mocha.Mediator.MediatorModuleAttribute";
}
