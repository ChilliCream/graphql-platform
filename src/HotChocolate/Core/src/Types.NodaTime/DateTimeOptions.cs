using HotChocolate.Properties;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// Defines options for configuring the behavior of date and time scalar types, such as
/// <c>DateTime</c>, <c>LocalDateTime</c>, and <c>LocalTime</c>.
/// </summary>
public struct DateTimeOptions
{
    public const byte DefaultInputPrecision = 9;
    public const byte DefaultOutputPrecision = 9;

    public DateTimeOptions()
    {
    }

    /// <summary>
    /// Gets the maximum number of fractional second digits to expect when parsing date and time
    /// input values.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is greater than 9.
    /// </exception>
    public byte InputPrecision
    {
        get;
        init
        {
            if (value > 9)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(InputPrecision),
                    value,
                    TypeResources.DateTimeOptions_InputPrecision_InvalidValue);
            }

            field = value;
        }
    } = DefaultInputPrecision;

    /// <summary>
    /// Gets the maximum number of fractional second digits to include when serializing date and
    /// time output values.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is greater than 9.
    /// </exception>
    public byte OutputPrecision
    {
        get;
        init
        {
            if (value > 9)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(OutputPrecision),
                    value,
                    NodaTimeResources.DateTimeOptions_OutputPrecision_InvalidValue);
            }

            field = value;
        }
    } = DefaultOutputPrecision;

    /// <summary>
    /// Gets a value indicating whether fractional seconds are always emitted in serialized output,
    /// padded with trailing zeros up to <see cref="OutputPrecision"/>. When <see langword="false"/>
    /// (the default), trailing zeros are stripped and the fractional component is omitted entirely
    /// when zero. Has no effect when <see cref="OutputPrecision"/> is <c>0</c>.
    /// </summary>
    public bool AlwaysOutputFractionalSeconds { get; init; }
}
