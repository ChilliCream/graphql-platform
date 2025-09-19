namespace HotChocolate.Validation;

/// <summary>
/// The exception is thrown whenever the max validation error is exceeded.
/// </summary>
public class MaxValidationErrorsException : Exception
{
    public MaxValidationErrorsException() { }

    public MaxValidationErrorsException(string message)
        : base(message) { }

    public MaxValidationErrorsException(string message, Exception inner)
        : base(message, inner) { }
}
