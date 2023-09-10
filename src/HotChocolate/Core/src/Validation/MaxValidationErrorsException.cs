using System;
using System.Runtime.Serialization;

namespace HotChocolate.Validation;

/// <summary>
/// The exception is thrown whenever the max validation error is exceeded.
/// </summary>
[Serializable]
public class MaxValidationErrorsException : Exception
{
    public MaxValidationErrorsException() { }

    public MaxValidationErrorsException(string message)
        : base(message) { }

    public MaxValidationErrorsException(string message, Exception inner)
        : base(message, inner) { }

    protected MaxValidationErrorsException(
        SerializationInfo info,
        StreamingContext context)
        : base(info, context) { }
}
