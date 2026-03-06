using HotChocolate.Properties;

namespace HotChocolate.Types;

/// <summary>
/// Defines options for configuring the behavior of date and time scalar types, such as
/// <c>DateTime</c>, <c>LocalDateTime</c>, and <c>LocalTime</c>.
/// </summary>
public struct DateTimeOptions
{
    public const byte DefaultInputPrecision = 7;
    public const byte DefaultOutputPrecision = 7;

    public DateTimeOptions()
    {
    }

    /// <summary>
    /// Gets the maximum number of fractional second digits to expect when parsing date and time
    /// input values.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is greater than 7.
    /// </exception>
    public byte InputPrecision
    {
        get;
        init
        {
            if (value > 7)
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
    /// Thrown when the value is greater than 7.
    /// </exception>
    public byte OutputPrecision
    {
        get;
        init
        {
            if (value > 7)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(OutputPrecision),
                    value,
                    TypeResources.DateTimeOptions_OutputPrecision_InvalidValue);
            }

            field = value;
        }
    } = DefaultOutputPrecision;

    /// <summary>
    /// Gets a value indicating whether the input format of date and time values should be validated
    /// against the expected format. Defaults to <c>true</c>.
    /// </summary>
    public bool ValidateInputFormat { get; init; } = true;
}
