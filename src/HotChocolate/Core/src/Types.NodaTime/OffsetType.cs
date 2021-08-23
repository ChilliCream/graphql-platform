using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime
{
    /// <summary>
    /// An offset from UTC in seconds.
    /// A positive value means that the local time is ahead of UTC (e.g. for Europe);
    /// a negative value means that the local time is behind UTC (e.g. for America).
    /// </summary>
    public class OffsetType : StringToStructBaseType<Offset>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="OffsetType"/>.
        /// </summary>
        public OffsetType() : base("Offset")
        {
            Description = NodaTimeResources.OffsetType_Description;
        }

        /// <inheritdoc />
        protected override string Serialize(Offset runtimeValue)
            => OffsetPattern.GeneralInvariantWithZ
                .WithCulture(CultureInfo.InvariantCulture)
                .Format(runtimeValue);

        /// <inheritdoc />
        protected override bool TryDeserialize(
            string resultValue,
            [NotNullWhen(true)] out Offset? runtimeValue)
            => OffsetPattern.GeneralInvariantWithZ
                .WithCulture(CultureInfo.InvariantCulture)
                .TryParse(resultValue, out runtimeValue);
    }
}
