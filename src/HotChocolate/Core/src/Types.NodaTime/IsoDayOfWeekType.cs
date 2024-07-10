using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// Equates the days of the week with their numerical value according to ISO-8601.
/// Monday = 1, Tuesday = 2, Wednesday = 3, Thursday = 4, Friday = 5, Saturday = 6, Sunday = 7.
/// </summary>
public class IsoDayOfWeekType : IntToStructBaseType<IsoDayOfWeek>
{
    /// <summary>
    /// Initializes a new instance of <see cref="IsoDayOfWeekType"/>.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public IsoDayOfWeekType() : base("IsoDayOfWeek")
    {
        Description = NodaTimeResources.IsoDayOfWeekType_Description;
    }

    /// <inheritdoc />
    protected override bool TrySerialize(
        IsoDayOfWeek runtimeValue,
        [NotNullWhen(true)] out int? resultValue)
    {
        if (runtimeValue == IsoDayOfWeek.None)
        {
            resultValue = null;
            return false;
        }

        resultValue = (int)runtimeValue;
        return true;
    }

    /// <inheritdoc />
    protected override bool TryDeserialize(
        int resultValue,
        [NotNullWhen(true)] out IsoDayOfWeek? runtimeValue)
    {
        if (resultValue < 1 || resultValue > 7)
        {
            runtimeValue = null;
            return false;
        }

        runtimeValue = (IsoDayOfWeek)resultValue;
        return true;
    }
}
