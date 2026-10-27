using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Tracks the request token that is canceled within <see cref="MaxTimeout"/> while it is set.
/// </summary>
internal sealed class ExecutionDeadline : RequestFeature
{
    /// <summary>
    /// The longest execution timeout that sets a deadline, which is the default <see cref="HttpClient.Timeout"/>.
    /// </summary>
    public static readonly TimeSpan MaxTimeout = TimeSpan.FromSeconds(100);

    private CancellationToken _token;

    /// <summary>
    /// Sets the token that is canceled within <see cref="MaxTimeout"/> from now.
    /// </summary>
    public void Set(CancellationToken token) => _token = token;

    /// <summary>
    /// Removes the token.
    /// </summary>
    public void Clear() => _token = default;

    /// <summary>
    /// Gets whether <paramref name="token"/> is the token that is canceled within <see cref="MaxTimeout"/>.
    /// </summary>
    public bool Covers(CancellationToken token) => _token.CanBeCanceled && _token.Equals(token);

    protected internal override void Reset() => _token = default;
}
