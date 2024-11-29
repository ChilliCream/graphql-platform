using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// A combination of a LocalDate and an Offset,
/// to represent a date at a specific offset from UTC but
/// without any time-of-day information.
/// </summary>
public class OffsetDateType : StringToStructBaseType<OffsetDate>
{
    private readonly IPattern<OffsetDate>[] _allowedPatterns;
    private readonly IPattern<OffsetDate> _serializationPattern;

    /// <summary>
    /// Initializes a new instance of <see cref="OffsetDateType"/>.
    /// </summary>
    public OffsetDateType(params IPattern<OffsetDate>[] allowedPatterns) : base("OffsetDate")
    {
        if (allowedPatterns.Length == 0)
        {
            throw ThrowHelper.PatternCannotBeEmpty(this);
        }

        _allowedPatterns = allowedPatterns;
        _serializationPattern = allowedPatterns[0];

        Description = CreateDescription(
            allowedPatterns,
            NodaTimeResources.OffsetDateType_Description,
            NodaTimeResources.OffsetDateType_Description_Extended);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="OffsetDateType"/>.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public OffsetDateType() : this(OffsetDatePattern.GeneralIso)
    {
    }

    /// <inheritdoc />
    protected override string Serialize(OffsetDate runtimeValue)
        => _serializationPattern
            .Format(runtimeValue);

    /// <inheritdoc />
    protected override bool TryDeserialize(
        string resultValue,
        [NotNullWhen(true)] out OffsetDate? runtimeValue)
        => _allowedPatterns.TryParse(resultValue, out runtimeValue);

    protected override Dictionary<IPattern<OffsetDate>, string> PatternMap => new()
    {
        { OffsetDatePattern.GeneralIso, "YYYY-MM-DD±hh:mm" },
        { OffsetDatePattern.FullRoundtrip, "YYYY-MM-DD±hh:mm (calendar)" }
    };

    protected override Dictionary<IPattern<OffsetDate>, string> ExampleMap => new()
    {
        { OffsetDatePattern.GeneralIso, "2000-01-01Z" },
        { OffsetDatePattern.FullRoundtrip, "2000-01-01Z (ISO)" }
    };
}
