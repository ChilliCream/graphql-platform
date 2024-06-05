using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// LocalDate is an immutable struct representing a date within the calendar,
/// with no reference to a particular time zone or time of day.
/// </summary>
public class LocalDateType : StringToStructBaseType<LocalDate>
{
    private readonly IPattern<LocalDate>[] _allowedPatterns;
    private readonly IPattern<LocalDate> _serializationPattern;

    /// <summary>
    /// Initializes a new instance of <see cref="LocalDateType"/>.
    /// </summary>
    public LocalDateType(params IPattern<LocalDate>[] allowedPatterns) : base("LocalDate")
    {
        if (allowedPatterns.Length == 0)
        {
            throw ThrowHelper.PatternCannotBeEmpty(this);
        }

        _allowedPatterns = allowedPatterns;
        _serializationPattern = allowedPatterns[0];
        Description = NodaTimeResources.LocalDateType_Description;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="LocalDateType"/>.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public LocalDateType() : this(LocalDatePattern.Iso)
    {
    }

    /// <inheritdoc />
    protected override string Serialize(LocalDate runtimeValue)
        => _serializationPattern
            .Format(runtimeValue);

    /// <inheritdoc />
    protected override bool TryDeserialize(
        string resultValue,
        [NotNullWhen(true)] out LocalDate? runtimeValue)
        => _allowedPatterns.TryParse(resultValue, out runtimeValue);
}
