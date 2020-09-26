using HotChocolate.Types.Spatial;

namespace HotChocolate.Types
{
    public static class GeoJsonSerializationExceptionExtensions
    {
        public static SerializationException ToSerializationException(
            this GeoJsonSerializationException exception,
            IType type) =>
            new SerializationException(
                ErrorBuilder.New().SetMessage(exception.Message).SetException(exception).Build(),
                type);
    }
}
