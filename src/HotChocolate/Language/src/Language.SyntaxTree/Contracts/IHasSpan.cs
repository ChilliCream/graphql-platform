using System;

namespace HotChocolate.Language;

/// <summary>
/// This interfaces is implemented by value literals to give access to the memory of the raw value.
/// </summary>
public interface IHasSpan
{
    /// <summary>
    /// Gets access to the raw value representation of a value literal syntax node.
    /// </summary>
    ReadOnlySpan<byte> AsSpan();
}
