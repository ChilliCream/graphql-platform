using System;
using System.Runtime.Serialization;

namespace HotChocolate.Utilities
{
    [Serializable]
    public class ServiceException : Exception
    {
        public ServiceException() { }

        public ServiceException(string message) 
            : base(message) { }

        public ServiceException(string message, Exception inner) 
            : base(message, inner) { }

        protected ServiceException(
            SerializationInfo info,
            StreamingContext context) 
            : base(info, context) { }
    }
}
