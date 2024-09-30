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
    [MemberNotNullWhen(false, nameof(Value))]
    bool IsSuccess { get; }

    /// <summary>
    /// Defines if the mutation had an error and if the result represents a error result.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    bool IsError { get; }
}
