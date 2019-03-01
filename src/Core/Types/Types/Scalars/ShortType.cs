using System.Globalization;

namespace HotChocolate.Types
{
    public sealed class ShortType
        : NumericTypeBase<short>
    {
        public ShortType()
            : base("Short")
        {
        }

        public override string Description =>
            TypeResourceHelper.ShortType_Description();

        protected override bool TryParseValue(string s, out short value) =>
            short.TryParse(
                s,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out value);

        protected override string SerializeValue(short value) =>
            value.ToString("D", CultureInfo.InvariantCulture);
    }
}
