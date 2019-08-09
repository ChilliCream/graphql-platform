using System;

namespace HotChocolate.Language
{
#if netstandard1_4
    [Serializable]
#endif
    public class Utf8EncodingException
        : Exception
    {
        public Utf8EncodingException() { }
        public Utf8EncodingException(string message)
            : base(message) { }
        public Utf8EncodingException(string message, Exception inner)
            : base(message, inner) { }
    }

}
