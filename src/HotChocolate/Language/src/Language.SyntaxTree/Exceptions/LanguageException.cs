using System;

namespace HotChocolate.Language;

public class LanguageException : Exception
{
    public LanguageException() { }

    public LanguageException(string message)
        : base(message) { }

    public LanguageException(string message, Exception inner)
        : base(message, inner) { }
}
