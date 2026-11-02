namespace Mocha.Sagas;

/// <summary>
/// Represents an event that carries a correlation identifier used to match it to an existing saga instance.
/// </summary>
public interface ICorrelatable
{
    /// <summary>
    /// Gets the correlation identifier used to look up the saga instance, or <c>null</c> if not correlated.
    /// </summary>
    Guid? CorrelationId { get; }
}
