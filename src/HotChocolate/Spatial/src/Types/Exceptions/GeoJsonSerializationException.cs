using System;

namespace HotChocolate.Types
{
    public class GeoJsonSerializationException : Exception
    {
        public GeoJsonSerializationException(string message) : base(message)
        {
        }

        public GeoJsonSerializationException(string message, params object[] args)
            : base(string.Format(message, args))
        {
        }
    }
}
