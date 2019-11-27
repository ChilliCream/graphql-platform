using System.Globalization;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    public sealed class ByteType
        : NumericTypeBase<byte>
    {
        public ByteType()
            : base("Byte")
        {
            Description = TypeResources.ByteType_Description;
        }

        protected override bool TryParseValue(string s, out byte value) =>
            byte.TryParse(
                s,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out value);

        protected override string SerializeValue(byte value) =>
            value.ToString("D", CultureInfo.InvariantCulture);
    }
}
