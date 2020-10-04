namespace HotChocolate.Types.Spatial
{
    public class GeoJsonSerializationException : GraphQLException
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
