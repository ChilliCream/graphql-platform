using System;
using System.Runtime.Serialization;

namespace HotChocolate.Types
{
    [Serializable]
    public class ScalarException : Exception
    {
        public ScalarException(string message) : base(message) { }
        public ScalarException(string message, Exception innerException)
            : base(message, innerException) { }
        protected ScalarException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }
    }
}
