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
}
