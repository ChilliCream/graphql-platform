using System.Runtime.CompilerServices;

namespace HotChocolate.Execution;

/// <summary>
/// Represents the condition flags evaluated for a request.
/// </summary>
public readonly struct ConditionFlags
{
    private readonly ulong[]? _overflow;

    /// <summary>
    /// Initializes a new instance of <see cref="ConditionFlags"/>.
    /// </summary>
    /// <param name="word0">The flags for condition indexes 0 through 63.</param>
    /// <param name="overflow">The flags for condition indexes 64 and above.</param>
    public ConditionFlags(ulong word0, ulong[]? overflow = null)
    {
        Word0 = word0;
        _overflow = overflow;
    }

    /// <summary>
    /// Gets the flags for condition indexes 0 through 63.
    /// </summary>
    public ulong Word0
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    }

    /// <summary>
    /// Gets the flags for condition indexes 64 and above.
    /// </summary>
    public ulong[]? Overflow
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _overflow;
    }
}
