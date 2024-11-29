using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// A local date and time in a particular calendar system, combined with an offset from UTC.
/// </summary>
public class OffsetDateTimeType : StringToStructBaseType<OffsetDateTime>
{
    private readonly IPattern<OffsetDateTime>[] _allowedPatterns;
    private readonly IPattern<OffsetDateTime> _serializationPattern;

    /// <summary>
    /// Initializes a new instance of <see cref="OffsetDateTimeType"/>.
    /// </summary>
    public OffsetDateTimeType(params IPattern<OffsetDateTime>[] allowedPatterns)
        : base("OffsetDateTime")
    {
        if (allowedPatterns.Length == 0)
        {
            throw ThrowHelper.PatternCannotBeEmpty(this);
        }

        _allowedPatterns = allowedPatterns;
        _serializationPattern = _allowedPatterns[0];

        Description = CreateDescription(
            allowedPatterns,
            NodaTimeResources.OffsetDateTimeType_Description,
            NodaTimeResources.OffsetDateTimeType_Description_Extended);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="OffsetDateTimeType"/>.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public OffsetDateTimeType() : this(OffsetDateTimePattern.ExtendedIso)
    {
        // Backwards compatibility with the original code's behavior
        _serializationPattern = OffsetDateTimePattern.GeneralIso;
        _allowedPatterns = [OffsetDateTimePattern.ExtendedIso,];
    }

    /// <inheritdoc />
    protected override string Serialize(OffsetDateTime runtimeValue)
        => _serializationPattern
            .Format(runtimeValue);

    /// <inheritdoc />
    protected override bool TryDeserialize(
        string resultValue,
        [NotNullWhen(true)] out OffsetDateTime? runtimeValue)
        => _allowedPatterns.TryParse(resultValue, out runtimeValue);

    protected override Dictionary<IPattern<OffsetDateTime>, string> PatternMap => new()
    {
        { OffsetDateTimePattern.GeneralIso, "YYYY-MM-DDThh:mm:ss±hh:mm" },
        { OffsetDateTimePattern.ExtendedIso, "YYYY-MM-DDThh:mm:ss.sssssssss±hh:mm" },
        { OffsetDateTimePattern.Rfc3339, "YYYY-MM-DDThh:mm:ss.sssssssss±hh:mm" },
        { OffsetDateTimePattern.FullRoundtrip, "YYYY-MM-DDThh:mm:ss.sssssssss±hh:mm (calendar)" }
    };

    protected override Dictionary<IPattern<OffsetDateTime>, string> ExampleMap => new()
    {
        { OffsetDateTimePattern.GeneralIso, "2000-01-01T20:00:00Z" },
        { OffsetDateTimePattern.ExtendedIso, "2000-01-01T20:00:00.999Z" },
        { OffsetDateTimePattern.Rfc3339, "2000-01-01T20:00:00.999Z" },
        { OffsetDateTimePattern.FullRoundtrip, "2000-01-01T20:00:00.999Z (ISO)" }
    };
}
