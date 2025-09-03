using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// AnnualDate is an immutable struct representing a date within the calendar,
/// with no reference to a particular time zone, year, or time.
/// </summary>
public class AnnualDateType : StringToStructBaseType<AnnualDate>
{
    private readonly IPattern<AnnualDate>[] _allowedPatterns;
    private readonly IPattern<AnnualDate> _serializationPattern;

    /// <summary>
    /// Initializes a new instance of <see cref="AnnualDateType"/>.
    /// </summary>
    public AnnualDateType(params IPattern<AnnualDate>[] allowedPatterns) : base("AnnualDate")
    {
        if (allowedPatterns.Length == 0)
        {
            throw ThrowHelper.PatternCannotBeEmpty(this);
        }

        _allowedPatterns = allowedPatterns;
        _serializationPattern = allowedPatterns[0];

        Description = CreateDescription(
            allowedPatterns,
            NodaTimeResources.AnnualDateType_Description,
            NodaTimeResources.AnnualDateType_Description_Extended);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AnnualDateType"/>.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public AnnualDateType() : this(AnnualDatePattern.Iso)
    {
    }

    /// <inheritdoc />
    protected override string Serialize(AnnualDate runtimeValue)
        => _serializationPattern
            .Format(runtimeValue);

    /// <inheritdoc />
    protected override bool TryDeserialize(
        string resultValue,
        [NotNullWhen(true)] out AnnualDate? runtimeValue)
        => _allowedPatterns.TryParse(resultValue, out runtimeValue);

    protected override Dictionary<IPattern<AnnualDate>, string> PatternMap => new()
    {
        { AnnualDatePattern.Iso, "MM-DD" }
    };

    protected override Dictionary<IPattern<AnnualDate>, string> ExampleMap => new()
    {
        { AnnualDatePattern.Iso, "01-01" }
    };
}
