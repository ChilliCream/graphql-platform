using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

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
    public override IValueNode ParseResult(object? resultValue)
    {
        return resultValue switch
        {
            null => NullValueNode.Default,

            string s when OffsetLookup.TryDeserialize(s, out var timeSpan) =>
                ParseValue(timeSpan),

            TimeSpan ts => ParseValue(ts),

            _ => throw ThrowHelper.UtcOffset_ParseValue_IsInvalid(this),
        };
    }

    /// <inheritdoc />
    protected override TimeSpan ParseLiteral(StringValueNode valueSyntax)
    {
        if (OffsetLookup.TryDeserialize(valueSyntax.Value, out var parsed))
        {
            return parsed;
        }

        throw ThrowHelper.UtcOffset_ParseLiteral_IsInvalid(this);
    }

    /// <inheritdoc />
    protected override StringValueNode ParseValue(TimeSpan runtimeValue)
    {
        if (OffsetLookup.TrySerialize(runtimeValue, out var serialized))
        {
            return new StringValueNode(serialized);
        }

        throw ThrowHelper.UtcOffset_ParseValue_IsInvalid(this);
    }

    /// <inheritdoc />
    public override bool TrySerialize(object? runtimeValue, out object? resultValue)
    {
        switch (runtimeValue)
        {
            case null:
                resultValue = null;
                return true;
            case TimeSpan timeSpan when OffsetLookup.TrySerialize(timeSpan, out var s):
                resultValue = s;
                return true;
            default:
                resultValue = null;
                return false;
        }
    }

    /// <inheritdoc />
    public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
    {
        switch (resultValue)
        {
            case null:
                runtimeValue = null;
                return true;
            case string s when OffsetLookup.TryDeserialize(s, out var timeSpan):
                runtimeValue = timeSpan;
                return true;
            case TimeSpan timeSpan when OffsetLookup.TrySerialize(timeSpan, out _):
                runtimeValue = timeSpan;
                return true;
            default:
                runtimeValue = null;
                return false;
        }
    }

    private static class OffsetLookup
    {
        private static readonly IReadOnlyDictionary<TimeSpan, string> _timeSpanToOffset;
        private static readonly IReadOnlyDictionary<string, TimeSpan> _offsetToTimeSpan;

        static OffsetLookup()
        {
            _timeSpanToOffset = new Dictionary<TimeSpan, string>
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
                    { new TimeSpan(14, 0, 0), "+14:00" },
                };

            var offsetToTimeSpan = _timeSpanToOffset
                .Reverse()
                .ToDictionary(x => x.Value, x => x.Key);
            offsetToTimeSpan["-00:00"] = TimeSpan.Zero;
            offsetToTimeSpan["00:00"] = TimeSpan.Zero;

            _offsetToTimeSpan = offsetToTimeSpan;
        }

        public static bool TrySerialize(
            TimeSpan value,
            [NotNullWhen(true)] out string? result)
        {
            return _timeSpanToOffset.TryGetValue(value, out result);
        }

        public static bool TryDeserialize(
            string value,
            [NotNullWhen(true)] out TimeSpan result)
        {
            return _offsetToTimeSpan.TryGetValue(value, out result);
        }
    }
}
