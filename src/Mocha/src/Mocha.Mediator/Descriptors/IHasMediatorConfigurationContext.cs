namespace Mocha.Mediator;

/// <summary>
/// Indicates that a descriptor or extension has access to the mediator configuration context.
/// </summary>
public interface IHasMediatorConfigurationContext
{
    /// <summary>
    /// The descriptor context.
    /// </summary>
    IMediatorConfigurationContext Context { get; }
}
