namespace HotChocolate.Pagination.Serialization;

public static class CursorKeySerializerRegistration
{
    private static readonly object _sync = new();

    private static ICursorKeySerializer[] _serializers =
    [
        new IntCursorKeySerializer(),
        new GuidCursorKeySerializer(),
        new StringCursorKeySerializer(),
        new ShortCursorKeySerializer(),
        new LongCursorKeySerializer(),
        new UShortCursorKeySerializer(),
        new UIntCursorKeySerializer(),
        new ULongCursorKeySerializer(),
    ];

    public static ICursorKeySerializer Find(Type keyType)
    {
        var serializers = _serializers.AsSpan();
        foreach (var serializer in serializers)
        {
            if (serializer.IsSupported(keyType))
            {
                return serializer;
            }
        }

        throw new NotSupportedException($"The key type `{keyType.FullName ?? keyType.Name}` is not supported.");
    }

    public static void Register(ICursorKeySerializer serializer)
    {
        lock (_sync)
        {
            var buffer = new ICursorKeySerializer[_serializers.Length + 1];
            Array.Copy(_serializers, 0, buffer, 0, _serializers.Length);
            buffer[_serializers.Length] = serializer;
            _serializers = buffer;
        }
    }

    public static void Register(params ICursorKeySerializer[] serializers)
    {
        if (serializers.Length == 0)
        {
            return;
        }

        lock (_sync)
        {
            var buffer = new ICursorKeySerializer[_serializers.Length + serializers.Length];
            Array.Copy(_serializers, 0, buffer, 0, _serializers.Length);
            Array.Copy(serializers, 0, buffer, _serializers.Length, serializers.Length);
            _serializers = buffer;
        }
    }
}
