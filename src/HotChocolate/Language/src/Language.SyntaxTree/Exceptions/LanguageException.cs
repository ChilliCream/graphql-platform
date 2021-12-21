using System;
using System.Runtime.Serialization;

namespace HotChocolate.Language;

[Serializable]
public class LanguageException : Exception
{
    public LanguageException() { }
    public LanguageException(string message)
        : base(message) { }
    public LanguageException(string message, Exception inner)
        : base(message, inner) { }
    protected LanguageException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}
