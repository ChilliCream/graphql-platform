namespace HotChocolate;

/// <summary>
/// This interface allows middleware to access the mutation result value in an generic way.
/// </summary>
public interface IMutationResult
{
    /// <summary>
    /// Gets the mutation result value.
    /// </summary>
    object Value { get; }

    /// <summary>
    /// Defines if the mutation was successful and if the result represents a success result.
    /// </summary>
    bool IsSuccess { get; }
}
