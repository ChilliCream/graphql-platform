using System;

namespace HotChocolate.Utilities
{
#if !NETSTANDARD1_4
    [Serializable]
#endif
    public class CreateServiceException
        : Exception
    {
        public CreateServiceException()
        {
        }

        public CreateServiceException(string message)
            : base(message)
        {
        }

        public CreateServiceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
