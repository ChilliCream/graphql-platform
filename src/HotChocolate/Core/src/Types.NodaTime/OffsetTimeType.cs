using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// A combination of a LocalTime and an Offset, to represent a time-of-day at a specific offset
/// from UTC but without any date information.
/// </summary>
public class OffsetTimeType : StringToStructBaseType<OffsetTime>
{
    private readonly IPattern<OffsetTime>[] _allowedPatterns;
    private readonly IPattern<OffsetTime> _serializationPattern;

    /// <summary>
    /// Initializes a new instance of <see cref="OffsetTimeType"/>.
    /// </summary>
    public OffsetTimeType(params IPattern<OffsetTime>[] allowedPatterns) : base("OffsetTime")
    {
        if (allowedPatterns.Length == 0)
        {
            throw ThrowHelper.PatternCannotBeEmpty(this);
        }

        _allowedPatterns = allowedPatterns;
        _serializationPattern = _allowedPatterns[0];

        Description = CreateDescription(
            allowedPatterns,
            NodaTimeResources.OffsetTimeType_Description,
            NodaTimeResources.OffsetTimeType_Description_Extended);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="OffsetTimeType"/>.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public OffsetTimeType() : this(OffsetTimePattern.GeneralIso)
    {
    }

    /// <inheritdoc />
    protected override string Serialize(OffsetTime runtimeValue)
        => _serializationPattern
            .Format(runtimeValue);

    /// <inheritdoc />
    protected override bool TryDeserialize(
        string resultValue,
        [NotNullWhen(true)] out OffsetTime? runtimeValue)
        => _allowedPatterns.TryParse(resultValue, out runtimeValue);

    protected override Dictionary<IPattern<OffsetTime>, string> PatternMap => new()
    {
        { OffsetTimePattern.GeneralIso, "hh:mm:ss±hh:mm" },
        { OffsetTimePattern.ExtendedIso, "hh:mm:ss.sssssssss±hh:mm" },
        { OffsetTimePattern.Rfc3339, "hh:mm:ss.sssssssss±hh:mm" }
    };

    protected override Dictionary<IPattern<OffsetTime>, string> ExampleMap => new()
    {
        { OffsetTimePattern.GeneralIso, "20:00:00Z" },
        { OffsetTimePattern.ExtendedIso, "20:00:00.999Z" },
        { OffsetTimePattern.Rfc3339, "20:00:00.999999999Z" }
    };
}
