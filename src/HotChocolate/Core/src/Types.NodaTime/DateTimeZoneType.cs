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
    public DateTimeZoneType() : base("DateTimeZone")
    {
        Description = NodaTimeResources.DateTimeZoneType_Description;
    }

    /// <inheritdoc />
    protected override string Serialize(DateTimeZone runtimeValue)
        => runtimeValue.Id;

    /// <inheritdoc />
    protected override bool TryDeserialize(
        string resultValue,
        [NotNullWhen(true)] out DateTimeZone? runtimeValue)
    {
        DateTimeZone? result = DateTimeZoneProviders.Tzdb.GetZoneOrNull(resultValue);

        if (result == null)
        {
            runtimeValue = null;
            return false;
        }

        runtimeValue = result;
        return true;
    }
}
