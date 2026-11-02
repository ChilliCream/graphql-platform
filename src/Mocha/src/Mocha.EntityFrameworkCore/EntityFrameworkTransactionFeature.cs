using Mocha.Mediator;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Feature that carries the resolved Entity Framework transaction configuration
/// through the mediator's feature collection at pipeline compilation time.
/// </summary>
internal sealed class EntityFrameworkTransactionFeature
{
    /// <summary>
    /// Gets the <see cref="Type"/> of the <see cref="Microsoft.EntityFrameworkCore.DbContext"/>
    /// to use for transaction management.
    /// </summary>
    public required Type ContextType { get; init; }

    /// <summary>
    /// Gets an optional delegate that determines whether a transaction should be created
    /// for the given mediator context. When <see langword="null"/>, the default policy is used.
    /// </summary>
    public Func<IMediatorContext, bool>? ShouldCreateTransaction { get; init; }
}
