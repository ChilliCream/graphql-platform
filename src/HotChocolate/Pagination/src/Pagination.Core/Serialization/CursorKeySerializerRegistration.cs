namespace HotChocolate.Pagination.Serialization;

/// <summary>
/// Allows to register and resolve <see cref="ICursorKeySerializer"/>s.
/// </summary>
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
        new DateTimeOffsetCursorKeySerializer(),
        new DateTimeCursorKeySerializer(),
        new DateOnlyCursorKeySerializer(),
        new TimeOnlyCursorKeySerializer(),
        new DecimalCursorKeySerializer(),
        new DoubleCursorKeySerializer(),
        new FloatCursorKeySerializer(),
        new BoolCursorKeySerializer(),
        new UShortCursorKeySerializer(),
        new UIntCursorKeySerializer(),
        new ULongCursorKeySerializer(),
    ];

    /// <summary>
    /// Find a <see cref="ICursorKeySerializer"/> for the given key type.
    /// </summary>
    /// <param name="keyType">
    /// The key type for which to find a <see cref="ICursorKeySerializer"/>.
    /// </param>
    /// <returns>
    /// Returns a <see cref="ICursorKeySerializer"/> for the given key type.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Throws if no <see cref="ICursorKeySerializer"/> was found for the given key type.
    /// </exception>
    public static ICursorKeySerializer Find(Type keyType)
    {
        // ReSharper disable once InconsistentlySynchronizedField
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

    /// <summary>
    /// Registers a <see cref="ICursorKeySerializer"/>.
    /// </summary>
    /// <param name="serializer">
    /// The <see cref="ICursorKeySerializer"/> to register.
    /// </param>
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

    /// <summary>
    /// Registers multiple <see cref="ICursorKeySerializer"/>s.
    /// </summary>
    /// <param name="serializers">
    /// The <see cref="ICursorKeySerializer"/>s to register.
    /// </param>
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
