using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// Represents a fixed (and calendar-independent) length of time.
/// </summary>
public class DurationType : StringToStructBaseType<Duration>
{
    private readonly IPattern<Duration>[] _allowedPatterns;
    private readonly IPattern<Duration> _serializationPattern;

    /// <summary>
    /// Initializes a new instance of <see cref="DurationType"/>.
    /// </summary>
    public DurationType(params IPattern<Duration>[] allowedPatterns) : base("Duration")
    {
        if (allowedPatterns.Length == 0)
        {
            throw ThrowHelper.PatternCannotBeEmpty(this);
        }

        _allowedPatterns = allowedPatterns;
        _serializationPattern = allowedPatterns[0];

        Description = CreateDescription(
            allowedPatterns,
            NodaTimeResources.DurationType_Description,
            NodaTimeResources.DurationType_Description_Extended);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DurationType"/>.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public DurationType() : this(DurationPattern.Roundtrip)
    {
    }

    /// <inheritdoc />
    protected override string Serialize(Duration runtimeValue)
        => _serializationPattern
            .Format(runtimeValue);

    /// <inheritdoc />
    protected override bool TryDeserialize(
        string resultValue,
        [NotNullWhen(true)] out Duration? runtimeValue)
        => _allowedPatterns.TryParse(resultValue, out runtimeValue);

    protected override Dictionary<IPattern<Duration>, string> PatternMap => new()
    {
        { DurationPattern.Roundtrip, "-D:hh:mm:ss.sssssssss" },
        { DurationPattern.JsonRoundtrip, "-hh:mm:ss.sssssssss" }
    };

    protected override Dictionary<IPattern<Duration>, string> ExampleMap => new()
    {
        { DurationPattern.Roundtrip, "-1:20:00:00.999999999" },
        { DurationPattern.JsonRoundtrip, "-44:00:00.999999999" }
    };
}
