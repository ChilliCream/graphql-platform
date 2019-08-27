using System;

namespace HotChocolate.Utilities
{
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
