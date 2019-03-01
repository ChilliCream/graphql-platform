using System.Globalization;

namespace HotChocolate.Types
{
    public sealed class ByteType
        : NumericTypeBase<byte>
    {
        public ByteType()
            : base("Byte")
        {
        }

        public override string Description =>
            TypeResourceHelper.ByteType_Description();

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
