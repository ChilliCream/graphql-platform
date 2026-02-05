using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// A calendar system maps the non-calendar-specific "local time line" to human concepts such as years, months and days.
/// </summary>
public class CalendarSystemType : StringToClassBaseType<CalendarSystem>
{
    /// <summary>
    /// Initializes a new instance of <see cref="CalendarSystemType"/>.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public CalendarSystemType() : base("CalendarSystem")
    {
        Description = NodaTimeResources.CalendarSystemType_Description;
    }

    /// <inheritdoc />
    protected override string Serialize(CalendarSystem runtimeValue)
        => runtimeValue.Id;

    /// <inheritdoc />
    protected override bool TryDeserialize(
        string resultValue,
        [NotNullWhen(true)] out CalendarSystem? runtimeValue
    )
    {
        // unfortunately this needs to be tried and caught because there isn't a way to safely try get otherwise
        try
        {
            runtimeValue = CalendarSystem.ForId(resultValue);
            return true;
        }
        catch
        {
            runtimeValue = null;
            return false;
        }
    }
}
