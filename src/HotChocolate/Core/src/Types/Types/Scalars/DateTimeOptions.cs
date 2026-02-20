using HotChocolate.Properties;

namespace HotChocolate.Types;

/// <summary>
/// <para>
/// Defines options for configuring the behavior of date and time scalar types, such as
/// <c>DateTime</c>, <c>LocalDateTime</c>, and <c>LocalTime</c>.
/// </para>
/// <para>
/// These options allow you to specify the precision of fractional seconds for both input parsing
/// and output serialization, ensuring that the date and time values are handled with the desired
/// level of detail.
/// </para>
/// <para>
/// The default precision is set to 7, which corresponds to the maximum precision supported by
/// .NET's date and time types. Adjusting these options can help optimize performance and storage
/// when high precision is not required, while still adhering to the GraphQL specification for date
/// and time formats.
/// </para>
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
}
