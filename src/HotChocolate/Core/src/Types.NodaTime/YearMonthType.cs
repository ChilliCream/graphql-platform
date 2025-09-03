using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// YearMonth is an immutable struct representing a month within the calendar,
/// with no reference to a particular time zone, date, or time.
/// </summary>
public class YearMonthType : StringToStructBaseType<YearMonth>
{
    private readonly IPattern<YearMonth>[] _allowedPatterns;
    private readonly IPattern<YearMonth> _serializationPattern;

    /// <summary>
    /// Initializes a new instance of <see cref="YearMonthType"/>.
    /// </summary>
    public YearMonthType(params IPattern<YearMonth>[] allowedPatterns) : base("YearMonth")
    {
        if (allowedPatterns.Length == 0)
        {
            throw ThrowHelper.PatternCannotBeEmpty(this);
        }

        _allowedPatterns = allowedPatterns;
        _serializationPattern = allowedPatterns[0];

        Description = CreateDescription(
            allowedPatterns,
            NodaTimeResources.YearMonthType_Description,
            NodaTimeResources.YearMonthType_Description_Extended);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="YearMonthType"/>.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public YearMonthType() : this(YearMonthPattern.Iso)
    {
    }

    /// <inheritdoc />
    protected override string Serialize(YearMonth runtimeValue)
        => _serializationPattern
            .Format(runtimeValue);

    /// <inheritdoc />
    protected override bool TryDeserialize(
        string resultValue,
        [NotNullWhen(true)] out YearMonth? runtimeValue)
        => _allowedPatterns.TryParse(resultValue, out runtimeValue);

    protected override Dictionary<IPattern<YearMonth>, string> PatternMap => new()
    {
        { YearMonthPattern.Iso, "YYYY-MM" }
    };

    protected override Dictionary<IPattern<YearMonth>, string> ExampleMap => new()
    {
        { YearMonthPattern.Iso, "2000-01" }
    };
}
