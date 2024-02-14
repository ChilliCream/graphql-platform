using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// Represents an instant on the global timeline, with nanosecond resolution.
/// </summary>
public class InstantType : StringToStructBaseType<Instant>
{
    private readonly IPattern<Instant>[] _allowedPatterns;
    private readonly IPattern<Instant> _serializationPattern;

    /// <summary>
    /// Initializes a new instance of <see cref="InstantType"/>.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public InstantType() : this(InstantPattern.ExtendedIso)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="InstantType"/>.
    /// </summary>
    public InstantType(params IPattern<Instant>[] allowedPatterns) : base("Instant")
    {
        if (allowedPatterns.Length == 0)
        {
            throw ThrowHelper.PatternCannotBeEmpty(this);
        }

        _allowedPatterns = allowedPatterns;
        _serializationPattern = allowedPatterns[0];
        Description = NodaTimeResources.InstantType_Description;
    }

    /// <inheritdoc />
    protected override string Serialize(Instant runtimeValue)
        => _serializationPattern
            .Format(runtimeValue);

    /// <inheritdoc />
    protected override bool TryDeserialize(
        string resultValue,
        [NotNullWhen(true)] out Instant? runtimeValue)
        => _allowedPatterns.TryParse(resultValue, out runtimeValue);
}
