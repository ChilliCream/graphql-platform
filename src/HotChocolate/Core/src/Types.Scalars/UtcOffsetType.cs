using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    /// <summary>
    /// The `UtcOffset` scalar type represents a value of format `±hh:mm`.
    /// </summary>
    public class UtcOffsetType : ScalarType<TimeSpan, StringValueNode>
    {
        private static readonly Dictionary<TimeSpan, string> _timeSpanToOffset =
            new()
            {
                {new TimeSpan(-12,0,0), "-12:00"},
                {new TimeSpan(-11,0,0), "-11:00"},
                {new TimeSpan(-10,0,0), "-10:00"},
                {new TimeSpan(-9,30,0), "-09:30"},
                {new TimeSpan(-9,0,0), "-09:00"},
                {new TimeSpan(-8,0,0), "-08:00"},
                {new TimeSpan(-7,0,0), "-07:00"},
                {new TimeSpan(-6,0,0), "-06:00"},
                {new TimeSpan(-5,0,0), "-05:00"},
                {new TimeSpan(-4,0,0), "-04:00"},
                {new TimeSpan(-3,30,0), "-03:30"},
                {new TimeSpan(-3,0,0), "-03:00"},
                {new TimeSpan(-2,0,0), "-02:00"},
                {new TimeSpan(-1,0,0), "-01:00"},
                {new TimeSpan(-1,0,0), "-01:00"},
                {new TimeSpan(0,0,0), "00:00"},
                {new TimeSpan(1,0,0), "+01:00"},
                {new TimeSpan(2,0,0), "+02:00"},
                {new TimeSpan(3,0,0), "+03:00"},
                {new TimeSpan(3,30,0), "+03:30"},
                {new TimeSpan(4,0,0), "+04:00"},
                {new TimeSpan(4,30,0), "+04:30"},
                {new TimeSpan(5,0,0), "+05:00"},
                {new TimeSpan(5,30,0), "+05:30"},
                {new TimeSpan(5,45,0), "+05:45"},
                {new TimeSpan(6,0,0), "+06:00"},
                {new TimeSpan(6,30,0), "+06:30"},
                {new TimeSpan(7,0,0), "+07:00"},
                {new TimeSpan(8,0,0), "+08:00"},
                {new TimeSpan(8,0,0), "+08:45"},
                {new TimeSpan(9,0,0), "+09:00"},
                {new TimeSpan(9,30,0), "+09:30"},
                {new TimeSpan(10,0,0), "+10:00"},
                {new TimeSpan(10,30,0), "+10:30"},
                {new TimeSpan(11,0,0), "+11:00"},
                {new TimeSpan(12,0,0), "+12:00"},
                {new TimeSpan(12,45,0), "+12:45"},
                {new TimeSpan(13,0,0), "+013:00"},
                {new TimeSpan(14,0,0), "+14:00"},
            };

        private static readonly Dictionary<string, TimeSpan> _offsetToTimeSpan =
            new() {["-12:00"] = new TimeSpan(-12, 0, 0)};

        /// <summary>
        /// Initializes a new instance of the <see cref="UtcOffsetType"/> class.
        /// </summary>
        public UtcOffsetType()
            : this(
                WellKnownScalarTypes.UtcOffset,
                ScalarResources.UtcOffsetType_Description,
                BindingBehavior.Implicit)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UtcOffsetType"/> class.
        /// </summary>
        public UtcOffsetType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, bind)
        {
            Description = description;
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            return resultValue switch
            {
                null => NullValueNode.Default,
                string s when _offsetToTimeSpan.TryGetValue(s, out TimeSpan timespan) => ParseValue(timespan),
                TimeSpan ts => ParseValue(ts),
                _ => throw ThrowHelper.UtcOffset_ParseValue_IsInvalid(this)
            };
        }

        protected override TimeSpan ParseLiteral(StringValueNode valueSyntax)
        {
            if (_offsetToTimeSpan.TryGetValue(valueSyntax.Value, out TimeSpan found))
            {
                return found;
            }

            throw ThrowHelper.UtcOffset_ParseLiteral_IsInvalid(this);
        }

        protected override StringValueNode ParseValue(TimeSpan runtimeValue)
        {
            if (_timeSpanToOffset.TryGetValue(runtimeValue, out var found))
            {
                return new StringValueNode(found);
            }

            throw ThrowHelper.UtcOffset_ParseValue_IsInvalid(this);
        }

        public override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            switch (runtimeValue)
            {
                case null:
                    resultValue = null;
                    return true;
                case TimeSpan timeSpan when _timeSpanToOffset.TryGetValue(timeSpan, out var s):
                    resultValue = s;
                    return true;
                default:
                    resultValue = null;
                    return false;
            }
        }

        public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
        {
            switch (resultValue)
            {
                case null:
                    runtimeValue = null;
                    return true;
                case string s when _offsetToTimeSpan.TryGetValue(s, out TimeSpan timeSpan):
                    runtimeValue = timeSpan;
                    return true;
                case TimeSpan ts:
                    runtimeValue = ts;
                    return true;
                default:
                    runtimeValue = null;
                    return false;
            }
        }
    }
}
