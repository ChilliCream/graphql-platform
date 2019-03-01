using System.Globalization;

namespace HotChocolate.Types
{
    public sealed class LongType
        : NumericTypeBase<long>
    {
        public LongType()
            : base("Long")
        {
        }

        public override string Description =>
            TypeResourceHelper.LongType_Description();

        protected override bool TryParseValue(string s, out long value) =>
            long.TryParse(
                s,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out value);

        protected override string SerializeValue(long value) =>
            value.ToString("D", CultureInfo.InvariantCulture);
    }
}
