namespace HotChocolate.Types.Spatial
{
    internal static class GeoJsonSerializationExceptionExtensions
    {
        public static SerializationException ToSerializationException(
            this GeoJsonSerializationException exception,
            IType type) =>
            new SerializationException(
                ErrorBuilder.New().SetMessage(exception.Message).SetException(exception).Build(),
                type);
    }
}
