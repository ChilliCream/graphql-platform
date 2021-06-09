using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime
{
    public class OffsetType : StringToStructBaseType<Offset>
    {
        public OffsetType() : base("Offset")
        {
            Description =
                "An offset from UTC in seconds.\n" +
                "A positive value means that the local time is ahead of UTC (e.g. for Europe); " +
                    "a negative value means that the local time is behind UTC (e.g. for America).";
        }

        protected override string Serialize(Offset runtimeValue)
            => OffsetPattern.GeneralInvariantWithZ
                .WithCulture(CultureInfo.InvariantCulture)
                .Format(runtimeValue);

        protected override bool TryDeserialize(string resultValue, [NotNullWhen(true)] out Offset? runtimeValue)
            => OffsetPattern.GeneralInvariantWithZ
                .WithCulture(CultureInfo.InvariantCulture)
                .TryParse(resultValue, out runtimeValue);
    }
}
