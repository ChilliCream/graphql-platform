using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// A LocalDateTime in a specific time zone and with a particular offset to distinguish between
/// otherwise-ambiguous instants.\nA ZonedDateTime is global, in that it maps to
/// a single Instant.
/// </summary>
public class ZonedDateTimeType : StringToStructBaseType<ZonedDateTime>
{
    private static readonly string _formatString = "uuuu'-'MM'-'dd'T'HH':'mm':'ss' 'z' 'o<g>";
    private static readonly ZonedDateTimePattern _default =
        ZonedDateTimePattern.CreateWithInvariantCulture(_formatString, DateTimeZoneProviders.Tzdb);

    private readonly IPattern<ZonedDateTime>[] _allowedPatterns;
    private readonly IPattern<ZonedDateTime> _serializationPattern;

    /// <summary>
    /// Initializes a new instance of <see cref="ZonedDateTimeType"/>.
    /// </summary>
    public ZonedDateTimeType(params IPattern<ZonedDateTime>[] allowedPatterns)
        : base("ZonedDateTime")
    {
        if (allowedPatterns.Length == 0)
        {
            throw ThrowHelper.PatternCannotBeEmpty(this);
        }

        _allowedPatterns = allowedPatterns;
        _serializationPattern = allowedPatterns[0];

        Description = CreateDescription(
            allowedPatterns,
            NodaTimeResources.ZonedDateTimeType_Description,
            NodaTimeResources.ZonedDateTimeType_Description_Extended);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ZonedDateTimeType"/>.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public ZonedDateTimeType() : this(_default)
    {
    }

    /// <inheritdoc />
    protected override string Serialize(ZonedDateTime runtimeValue)
        => _serializationPattern
            .Format(runtimeValue);

    /// <inheritdoc />
    protected override bool TryDeserialize(
        string resultValue,
        [NotNullWhen(true)] out ZonedDateTime? runtimeValue)
        => _allowedPatterns.TryParse(resultValue, out runtimeValue);

    protected override Dictionary<IPattern<ZonedDateTime>, string> PatternMap => new()
    {
        { ZonedDateTimePattern.GeneralFormatOnlyIso, "YYYY-MM-DDThh:mm:ss z (±hh:mm)" },
        { ZonedDateTimePattern.ExtendedFormatOnlyIso, "YYYY-MM-DDThh:mm:ss.sssssssss z (±hh:mm)" }
    };

    protected override Dictionary<IPattern<ZonedDateTime>, string> ExampleMap => new()
    {
        {
            ZonedDateTimePattern.GeneralFormatOnlyIso,
            "2000-01-01T20:00:00 Europe/Zurich (+01)"
        },
        {
            ZonedDateTimePattern.ExtendedFormatOnlyIso,
            "2000-01-01T20:00:00.999999999 Europe/Zurich (+01)"
        }
    };
}
