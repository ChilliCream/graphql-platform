using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class ByteTypeTests
        : NumberTypeTests<byte, ByteType, IntValueNode, byte>
    {
        protected override IntValueNode GetValueNode =>
            new IntValueNode("1");

        protected override IValueNode GetWrongValueNode =>
            new FloatValueNode("1.0f");

        protected override byte GetValue => 1;

        protected override object GetWrongValue => 1.0d;

        protected override byte GetAssertValue => 1;
        protected override byte GetSerializedAssertValue => 1;

        protected override byte GetMaxValue => byte.MaxValue;
        protected override string GetAssertMaxValue =>
            byte.MaxValue.ToString("D", CultureInfo.InvariantCulture);

        protected override byte GetMinValue => byte.MinValue;
        protected override string GetAssertMinValue =>
            byte.MinValue.ToString("D", CultureInfo.InvariantCulture);
    }
}
