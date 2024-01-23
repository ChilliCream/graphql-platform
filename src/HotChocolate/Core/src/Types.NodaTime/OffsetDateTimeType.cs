using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// A local date and time in a particular calendar system, combined with an offset from UTC.
/// </summary>
public class OffsetDateTimeType : StringToStructBaseType<OffsetDateTime>
{
    private readonly IPattern<OffsetDateTime>[] _allowedPatterns;
    private readonly IPattern<OffsetDateTime> _serializationPattern;

    /// <summary>
    /// Initializes a new instance of <see cref="OffsetDateTimeType"/>.
    /// </summary>
    public OffsetDateTimeType() : this(OffsetDateTimePattern.ExtendedIso)
    {
        // Backwards compatibility with the original code's behavior
        _serializationPattern = OffsetDateTimePattern.GeneralIso;
        _allowedPatterns = [OffsetDateTimePattern.ExtendedIso,];
    }

    /// <summary>
    /// Initializes a new instance of <see cref="OffsetDateTimeType"/>.
    /// </summary>
    public OffsetDateTimeType(params IPattern<OffsetDateTime>[] allowedPatterns)
        : base("OffsetDateTime")
    {
        if (allowedPatterns.Length == 0)
        {
            throw ThrowHelper.PatternCannotBeEmpty(this);
        }

        _allowedPatterns = allowedPatterns;
        _serializationPattern = _allowedPatterns[0];
        Description = NodaTimeResources.OffsetDateTimeType_Description;
    }

    /// <inheritdoc />
    protected override string Serialize(OffsetDateTime runtimeValue)
        => _serializationPattern
            .Format(runtimeValue);

    /// <inheritdoc />
    protected override bool TryDeserialize(
        string resultValue,
        [NotNullWhen(true)] out OffsetDateTime? runtimeValue)
        => _allowedPatterns.TryParse(resultValue, out runtimeValue);
}
