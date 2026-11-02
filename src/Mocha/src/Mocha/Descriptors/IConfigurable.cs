namespace Mocha;

/// <summary>
/// A descriptor that accepts configuration callbacks targeting <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The descriptor type the configuration callbacks receive.</typeparam>
public interface IConfigurable<T>
{
    /// <summary>
    /// Applies a configuration callback to the targeted descriptor, either immediately or when the
    /// target is configured.
    /// </summary>
    /// <param name="configure">The callback that configures the target.</param>
    void Configure(Action<T> configure);
}
