using System.Diagnostics.CodeAnalysis;

namespace HotChocolate;

/// <summary>
/// This interface allows middleware to access the mutation result value in an generic way.
/// </summary>
public interface IFieldResult
{
    /// <summary>
    /// Gets the mutation result value.
    /// </summary>
    object? Value { get; }

    /// <summary>
    /// Defines if the mutation was successful and if the result represents a success result.
    /// </summary>
#if NET5_0_OR_GREATER
    [MemberNotNullWhen(false, nameof(Value))]
#endif
    bool IsSuccess { get; }

    /// <summary>
    /// Defines if the mutation had an error and if the result represents a error result.
    /// </summary>
#if NET5_0_OR_GREATER
    [MemberNotNullWhen(true, nameof(Value))]
#endif
    bool IsError { get; }
}
