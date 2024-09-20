using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// A date and time in a particular calendar system.
/// </summary>
public class LocalDateTimeType : StringToStructBaseType<LocalDateTime>
{
    private readonly IPattern<LocalDateTime>[] _allowedPatterns;
    private readonly IPattern<LocalDateTime> _serializationPattern;

    /// <summary>
    /// Initializes a new instance of <see cref="LocalDateTimeType"/>.
    /// </summary>
    public LocalDateTimeType(params IPattern<LocalDateTime>[] allowedPatterns) : base("LocalDateTime")
    {
        if (allowedPatterns.Length == 0)
        {
            throw ThrowHelper.PatternCannotBeEmpty(this);
        }

        _allowedPatterns = allowedPatterns;
        _serializationPattern = allowedPatterns[0];
        Description = NodaTimeResources.LocalDateTimeType_Description;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="LocalDateTimeType"/>.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public LocalDateTimeType() : this(LocalDateTimePattern.ExtendedIso)
    {
    }

    /// <inheritdoc />
    protected override string Serialize(LocalDateTime runtimeValue)
        => _serializationPattern
            .Format(runtimeValue);

    /// <inheritdoc />
    protected override bool TryDeserialize(
        string resultValue,
        [NotNullWhen(true)] out LocalDateTime? runtimeValue)
        => _allowedPatterns.TryParse(resultValue, out runtimeValue);
}
