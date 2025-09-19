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

        Description = CreateDescription(
            allowedPatterns,
            NodaTimeResources.LocalDateType_Description,
            NodaTimeResources.LocalDateType_Description_Extended);
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

    protected override Dictionary<IPattern<LocalDate>, string> PatternMap => new()
    {
        { LocalDatePattern.Iso, "YYYY-MM-DD" },
        { LocalDatePattern.FullRoundtrip, "YYYY-MM-DD (calendar)" }
    };

    protected override Dictionary<IPattern<LocalDate>, string> ExampleMap => new()
    {
        { LocalDatePattern.Iso, "2000-01-01" },
        { LocalDatePattern.FullRoundtrip, "2000-01-01 (ISO)" }
    };
}
