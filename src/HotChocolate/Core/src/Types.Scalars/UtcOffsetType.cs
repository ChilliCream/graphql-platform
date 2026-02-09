using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

/// <summary>
/// The `UtcOffset` scalar type represents a value of format ±hh:mm.
/// </summary>
public class UtcOffsetType : ScalarType<TimeSpan, StringValueNode>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UtcOffsetType"/> class.
    /// </summary>
    public UtcOffsetType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Implicit)
        : base(name, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UtcOffsetType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public UtcOffsetType()
        : this(
            WellKnownScalarTypes.UtcOffset,
            ScalarResources.UtcOffsetType_Description)
    {
    }

    /// <inheritdoc />
    protected override TimeSpan OnCoerceInputLiteral(StringValueNode valueLiteral)
    {
        if (OffsetLookup.TryDeserialize(valueLiteral.Value, out var parsed))
        {
            return parsed;
        }

        throw ThrowHelper.UtcOffsetType_InvalidFormat(this);
    }

    /// <inheritdoc />
    protected override TimeSpan OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        if (OffsetLookup.TryDeserialize(inputValue.GetString()!, out var parsed))
        {
            return parsed;
        }

        throw ThrowHelper.UtcOffsetType_InvalidFormat(this);
    }

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(TimeSpan runtimeValue, ResultElement resultValue)
    {
        if (OffsetLookup.TrySerialize(runtimeValue, out var serialized))
        {
            resultValue.SetStringValue(serialized);
            return;
        }

        throw ThrowHelper.UtcOffsetType_InvalidFormat(this);
    }

    /// <inheritdoc />
    protected override StringValueNode OnValueToLiteral(TimeSpan runtimeValue)
    {
        if (OffsetLookup.TrySerialize(runtimeValue, out var serialized))
        {
            return new StringValueNode(serialized);
        }

        throw ThrowHelper.UtcOffsetType_InvalidFormat(this);
    }

    private static class OffsetLookup
    {
        private static readonly IReadOnlyDictionary<TimeSpan, string> s_timeSpanToOffset;
        private static readonly IReadOnlyDictionary<string, TimeSpan> s_offsetToTimeSpan;

        static OffsetLookup()
        {
            s_timeSpanToOffset = new Dictionary<TimeSpan, string>
                {
                    { new TimeSpan(-12, 0, 0), "-12:00" },
                    { new TimeSpan(-11, 0, 0), "-11:00" },
                    { new TimeSpan(-10, 0, 0), "-10:00" },
                    { new TimeSpan(-9, 30, 0), "-09:30" },
                    { new TimeSpan(-9, 0, 0), "-09:00" },
                    { new TimeSpan(-8, 0, 0), "-08:00" },
                    { new TimeSpan(-7, 0, 0), "-07:00" },
                    { new TimeSpan(-6, 0, 0), "-06:00" },
                    { new TimeSpan(-5, 0, 0), "-05:00" },
                    { new TimeSpan(-4, 0, 0), "-04:00" },
                    { new TimeSpan(-3, 30, 0), "-03:30" },
                    { new TimeSpan(-3, 0, 0), "-03:00" },
                    { new TimeSpan(-2, 0, 0), "-02:00" },
                    { new TimeSpan(-1, 0, 0), "-01:00" },
                    { TimeSpan.Zero, "+00:00" },
                    { new TimeSpan(1, 0, 0), "+01:00" },
                    { new TimeSpan(2, 0, 0), "+02:00" },
                    { new TimeSpan(3, 0, 0), "+03:00" },
                    { new TimeSpan(3, 30, 0), "+03:30" },
                    { new TimeSpan(4, 0, 0), "+04:00" },
                    { new TimeSpan(4, 30, 0), "+04:30" },
                    { new TimeSpan(5, 0, 0), "+05:00" },
                    { new TimeSpan(5, 30, 0), "+05:30" },
                    { new TimeSpan(5, 45, 0), "+05:45" },
                    { new TimeSpan(6, 0, 0), "+06:00" },
                    { new TimeSpan(6, 30, 0), "+06:30" },
                    { new TimeSpan(7, 0, 0), "+07:00" },
                    { new TimeSpan(8, 0, 0), "+08:00" },
                    { new TimeSpan(8, 45, 0), "+08:45" },
                    { new TimeSpan(9, 0, 0), "+09:00" },
                    { new TimeSpan(9, 30, 0), "+09:30" },
                    { new TimeSpan(10, 0, 0), "+10:00" },
                    { new TimeSpan(10, 30, 0), "+10:30" },
                    { new TimeSpan(11, 0, 0), "+11:00" },
                    { new TimeSpan(12, 0, 0), "+12:00" },
                    { new TimeSpan(12, 45, 0), "+12:45" },
                    { new TimeSpan(13, 0, 0), "+13:00" },
                    { new TimeSpan(14, 0, 0), "+14:00" }
                };

            var offsetToTimeSpan = s_timeSpanToOffset
                .Reverse()
                .ToDictionary(x => x.Value, x => x.Key);
            offsetToTimeSpan["-00:00"] = TimeSpan.Zero;
            offsetToTimeSpan["00:00"] = TimeSpan.Zero;

            s_offsetToTimeSpan = offsetToTimeSpan;
        }

        public static bool TrySerialize(
            TimeSpan value,
            [NotNullWhen(true)] out string? result)
            => s_timeSpanToOffset.TryGetValue(value, out result);

        public static bool TryDeserialize(string value, out TimeSpan result)
            => s_offsetToTimeSpan.TryGetValue(value, out result);
    }
}
