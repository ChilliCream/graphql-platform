using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// LocalTime is an immutable struct representing a time of day,
/// with no reference to a particular calendar, time zone or date.
/// </summary>
public class LocalTimeType : StringToStructBaseType<LocalTime>
{
    private readonly IPattern<LocalTime>[] _allowedPatterns;
    private readonly IPattern<LocalTime> _serializationPattern;

    /// <summary>
    /// Initializes a new instance of <see cref="LocalTimeType"/>.
    /// </summary>
    public LocalTimeType(params IPattern<LocalTime>[] allowedPatterns) : base("LocalTime")
    {
        if (allowedPatterns.Length == 0)
        {
            throw ThrowHelper.PatternCannotBeEmpty(this);
        }

        _allowedPatterns = allowedPatterns;
        _serializationPattern = allowedPatterns[0];

        Description = CreateDescription(
            allowedPatterns,
            NodaTimeResources.LocalTimeType_Description,
            NodaTimeResources.LocalTimeType_Description_Extended);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="LocalTimeType"/>.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public LocalTimeType() : this(LocalTimePattern.ExtendedIso)
    {
    }

    /// <inheritdoc />
    protected override string Serialize(LocalTime runtimeValue)
        => _serializationPattern
            .Format(runtimeValue);

    /// <inheritdoc />
    protected override bool TryDeserialize(
        string resultValue,
        [NotNullWhen(true)] out LocalTime? runtimeValue)
        => _allowedPatterns.TryParse(resultValue, out runtimeValue);

    protected override Dictionary<IPattern<LocalTime>, string> PatternMap => new()
    {
        { LocalTimePattern.ExtendedIso, "hh:mm:ss.sssssssss" },
        { LocalTimePattern.LongExtendedIso, "hh:mm:ss.sssssssss" },
        { LocalTimePattern.GeneralIso, "hh:mm:ss" }
    };

    protected override Dictionary<IPattern<LocalTime>, string> ExampleMap => new()
    {
        { LocalTimePattern.ExtendedIso, "20:00:00.999" },
        { LocalTimePattern.LongExtendedIso, "20:00:00.999999999" },
        { LocalTimePattern.GeneralIso, "20:00:00" }
    };
}
