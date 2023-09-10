using System;
using System.Runtime.Serialization;

namespace HotChocolate.Language;

[Serializable]
public class InvalidFormatException : LanguageException
{
    public InvalidFormatException() { }

    public InvalidFormatException(string message)
        : base(message) { }

    public InvalidFormatException(string message, Exception innerException)
        : base(message, innerException) { }

#if NET8_0_OR_GREATER
    [Obsolete(
        "This API supports obsolete formatter-based serialization. " +
        "It should not be called or extended by application code.",
        true)]
#endif
    protected InvalidFormatException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}
