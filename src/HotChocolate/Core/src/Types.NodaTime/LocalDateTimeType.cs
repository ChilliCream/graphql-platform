using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// A date and time in a particular calendar system.
/// </summary>
public class LocalDateTimeType : StringToStructBaseType<LocalDateTime>
{
    private readonly IPattern<LocalDateTime>[] _allowedPatterns;
    private readonly IPattern<LocalDateTime> _serializationPattern;

    /// <summary>
    /// Initializes a new instance of <see cref="LocalDateTimeType"/>.
    /// </summary>
    public LocalDateTimeType(params IPattern<LocalDateTime>[] allowedPatterns) : base("LocalDateTime")
    {
        if (allowedPatterns.Length == 0)
        {
            throw ThrowHelper.PatternCannotBeEmpty(this);
        }

        _allowedPatterns = allowedPatterns;
        _serializationPattern = allowedPatterns[0];

        Description = CreateDescription(
            _allowedPatterns,
            NodaTimeResources.LocalDateTimeType_Description,
            NodaTimeResources.LocalDateTimeType_Description_Extended);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="LocalDateTimeType"/>.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public LocalDateTimeType() : this(LocalDateTimePattern.ExtendedIso)
    {
    }

    /// <inheritdoc />
    protected override string Serialize(LocalDateTime runtimeValue)
        => _serializationPattern
            .Format(runtimeValue);

    /// <inheritdoc />
    protected override bool TryDeserialize(
        string resultValue,
        [NotNullWhen(true)] out LocalDateTime? runtimeValue)
        => _allowedPatterns.TryParse(resultValue, out runtimeValue);

    protected override Dictionary<IPattern<LocalDateTime>, string> PatternMap => new()
    {
        { LocalDateTimePattern.GeneralIso, "YYYY-MM-DDThh:mm:ss" },
        { LocalDateTimePattern.ExtendedIso, "YYYY-MM-DDThh:mm:ss.sssssssss" },
        { LocalDateTimePattern.BclRoundtrip, "YYYY-MM-DDThh:mm:ss.sssssss" },
        { LocalDateTimePattern.FullRoundtripWithoutCalendar, "YYYY-MM-DDThh:mm:ss.sssssssss" },
        { LocalDateTimePattern.FullRoundtrip, "YYYY-MM-DDThh:mm:ss.sssssssss (calendar)" }
    };

    protected override Dictionary<IPattern<LocalDateTime>, string> ExampleMap => new()
    {
        { LocalDateTimePattern.GeneralIso, "2000-01-01T20:00:00" },
        { LocalDateTimePattern.ExtendedIso, "2000-01-01T20:00:00.999" },
        { LocalDateTimePattern.BclRoundtrip, "2000-01-01T20:00:00.9999999" },
        { LocalDateTimePattern.FullRoundtripWithoutCalendar, "2000-01-01T20:00:00.999999999" },
        { LocalDateTimePattern.FullRoundtrip, "2000-01-01T20:00:00.999999999 (ISO)" }
    };
}
