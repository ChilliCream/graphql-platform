using System;

namespace HotChocolate.Language;

public class InvalidFormatException : LanguageException
{
    public InvalidFormatException() { }

    public InvalidFormatException(string message)
        : base(message) { }

    public InvalidFormatException(string message, Exception innerException)
        : base(message, innerException) { }
}
