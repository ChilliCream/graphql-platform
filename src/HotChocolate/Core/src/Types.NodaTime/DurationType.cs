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
    public DurationType() : this(DurationPattern.Roundtrip)
    {
    }

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
        Description = NodaTimeResources.DurationType_Description;
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
}
