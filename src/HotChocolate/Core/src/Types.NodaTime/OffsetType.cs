using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// An offset from UTC in seconds.
/// A positive value means that the local time is ahead of UTC (e.g. for Europe);
/// a negative value means that the local time is behind UTC (e.g. for America).
/// </summary>
public class OffsetType : StringToStructBaseType<Offset>
{
    private readonly IPattern<Offset>[] _allowedPatterns;
    private readonly IPattern<Offset> _serializationPattern;

    /// <summary>
    /// Initializes a new instance of <see cref="OffsetType"/>.
    /// </summary>
    public OffsetType(params IPattern<Offset>[] allowedPatterns) : base("Offset")
    {
        if (allowedPatterns.Length == 0)
        {
            throw ThrowHelper.PatternCannotBeEmpty(this);
        }

        _allowedPatterns = allowedPatterns;
        _serializationPattern = allowedPatterns[0];

        Description = CreateDescription(
            allowedPatterns,
            NodaTimeResources.OffsetType_Description,
            NodaTimeResources.OffsetType_Description_Extended);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="OffsetType"/>.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public OffsetType() : this(OffsetPattern.GeneralInvariantWithZ)
    {
    }

    /// <inheritdoc />
    protected override string Serialize(Offset runtimeValue)
        => _serializationPattern
            .Format(runtimeValue);

    /// <inheritdoc />
    protected override bool TryDeserialize(
        string resultValue,
        [NotNullWhen(true)] out Offset? runtimeValue)
        => _allowedPatterns.TryParse(resultValue, out runtimeValue);

    protected override Dictionary<IPattern<Offset>, string> PatternMap => new()
    {
        { OffsetPattern.GeneralInvariant, "±hh:mm:ss" },
        { OffsetPattern.GeneralInvariantWithZ, "Z" }
    };

    protected override Dictionary<IPattern<Offset>, string> ExampleMap => new()
    {
        { OffsetPattern.GeneralInvariant, "+02:30:00" },
        { OffsetPattern.GeneralInvariantWithZ, "Z" }
    };
}
