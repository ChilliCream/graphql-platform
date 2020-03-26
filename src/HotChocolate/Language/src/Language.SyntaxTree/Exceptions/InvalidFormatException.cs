using System;
using System.Runtime.Serialization;

namespace HotChocolate.Language
{
    [Serializable]
    public class InvalidFormatException
        : LanguageException
    {
        public InvalidFormatException() { }
        public InvalidFormatException(string message)
            : base(message) { }
        public InvalidFormatException(string message, Exception innerException)
            : base(message, innerException) { }
        protected InvalidFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
