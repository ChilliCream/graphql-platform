namespace StrawberryShake.Serialization;

public readonly record struct OptionalEnum<TEnum>
    where TEnum : struct, Enum
{
    private readonly string _rawValue;
    private readonly ILeafValueParser<string, TEnum> _parser;

    public OptionalEnum(string rawValue, ILeafValueParser<string, TEnum> parser)
    {
        _rawValue = rawValue;
        _parser = parser;
    }

    public TEnum Value => _parser.Parse(_rawValue);

    public bool IsUnknown
    {
        get
        {
            // Todo: Ugly, refactor
            try
            {
                _parser.Parse(_rawValue);
                return false;
            }
            catch
            {
                return true;
            }
        }
    }

    public static explicit operator TEnum?(OptionalEnum<TEnum> value)
        => value.IsUnknown ? null : value.Value;

    public static explicit operator TEnum(OptionalEnum<TEnum> value)
        => value.IsUnknown ? throw new ArgumentOutOfRangeException() : value.Value;
}
