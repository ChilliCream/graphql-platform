using Mocha.Mediator;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Options for configuring the Entity Framework Core mediator integration.
/// </summary>
public sealed class MediatorEntityFrameworkOptions
{
    /// <summary>
    /// Gets or sets the <see cref="Type"/> of the <see cref="Microsoft.EntityFrameworkCore.DbContext"/>
    /// to use for transaction management.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when accessed before calling
    /// <see cref="MediatorBuilderEntityFrameworkExtensions.UseEntityFrameworkTransactions{TContext}(IMediatorHostBuilder, Action{MediatorEntityFrameworkOptions}?)"/>.
    /// </exception>
    public Type ContextType
    {
        get => field ??
            throw new InvalidOperationException(
                "ContextType has not been configured. Call UseEntityFrameworkTransactions<TContext>() on the mediator builder.");
        set;
    }

    /// <summary>
    /// Gets or sets a delegate that determines whether a transaction should be created
    /// for the given mediator context. When <see langword="null"/>, the default policy is used
    /// which creates transactions for commands but not for queries.
    /// </summary>
    public Func<IMediatorContext, bool>? ShouldCreateTransaction { get; set; }
}
