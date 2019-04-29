using System;

namespace HotChocolate.Language
{
    [Serializable]
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
