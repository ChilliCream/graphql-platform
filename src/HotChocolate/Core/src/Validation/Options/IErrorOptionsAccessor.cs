namespace HotChocolate.Validation.Options;

/// <summary>
/// Represents the validation error options.
/// </summary>
public interface IErrorOptionsAccessor
{
    /// <summary>
    /// Specifies how many errors are allowed before the validation is aborted.
    /// </summary>
    int MaxAllowedErrors { get; }

    /// <summary>
    /// Specifies the maximum number of locations added to a validation error.
    /// </summary>
    int MaxLocationsPerError { get; }
}
