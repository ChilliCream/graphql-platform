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
                    TypeResources.DateTimeOptions_OutputPrecision_InvalidValue);
            }

            field = value;
        }
    } = DefaultOutputPrecision;
}
