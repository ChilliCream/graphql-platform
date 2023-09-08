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

#if NET8_0_OR_GREATER
    [Obsolete(
        "This API supports obsolete formatter-based serialization. " +
        "It should not be called or extended by application code.",
        true)]
#endif
    protected LanguageException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}
