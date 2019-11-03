using System;
#if netstandard1_4
using System.Runtime.Serialization;
#endif

namespace HotChocolate.Language
{
#if netstandard1_4
    [Serializable]
#endif
    public class InvalidFormatException
        : Exception
    {
        public InvalidFormatException() { }
        public InvalidFormatException(string message)
            : base(message) { }
        public InvalidFormatException(string message, Exception innerException)
            : base(message, innerException) { }

#if netstandard1_4
        protected InvalidFormatException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }
#endif
    }
}
