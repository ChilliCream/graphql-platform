using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// Represents a period of time expressed in human chronological terms:
/// hours, days, weeks, months and so on.
/// </summary>
public class PeriodType : StringToClassBaseType<Period>
{
    private readonly IPattern<Period>[] _allowedPatterns;
    private readonly IPattern<Period> _serializationPattern;

    /// <summary>
    /// Initializes a new instance of <see cref="PeriodType"/>.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public PeriodType() : this(PeriodPattern.Roundtrip)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="PeriodType"/>.
    /// </summary>
    public PeriodType(params IPattern<Period>[] allowedPatterns) : base("Period")
    {
        if (allowedPatterns.Length == 0)
        {
            throw ThrowHelper.PatternCannotBeEmpty(this);
        }

        _allowedPatterns = allowedPatterns;
        _serializationPattern = allowedPatterns[0];
        Description = NodaTimeResources.PeriodType_Description;
    }

    /// <inheritdoc />
    protected override string Serialize(Period runtimeValue)
        => _serializationPattern.Format(runtimeValue);

    /// <inheritdoc />
    protected override bool TryDeserialize(
        string resultValue,
        [NotNullWhen(true)] out Period? runtimeValue)
        => _allowedPatterns.TryParse(resultValue, out runtimeValue);
}
