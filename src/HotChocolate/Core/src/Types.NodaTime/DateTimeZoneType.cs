using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// Represents a time zone - a mapping between UTC and local time.
/// A time zone maps UTC instants to local times - or, equivalently,
/// to the offset from UTC at any particular instant.
/// </summary>
public class DateTimeZoneType : StringToClassBaseType<DateTimeZone>
{
    /// <summary>
    /// Initializes a new instance of <see cref="DateTimeZoneType"/>.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public DateTimeZoneType() : base("DateTimeZone")
    {
        Description = NodaTimeResources.DateTimeZoneType_Description;
    }

    /// <inheritdoc />
    protected override bool TryCoerceRuntimeValue(
        string resultValue,
        [NotNullWhen(true)] out DateTimeZone? runtimeValue)
    {
        runtimeValue = DateTimeZoneProviders.Tzdb.GetZoneOrNull(resultValue);
        return runtimeValue is not null;
    }

    /// <inheritdoc />
    protected override bool TryCoerceOutputValue(
        DateTimeZone runtimeValue,
        [NotNullWhen(true)] out string? resultValue)
    {
        resultValue = runtimeValue.Id;
        return true;
    }
}
