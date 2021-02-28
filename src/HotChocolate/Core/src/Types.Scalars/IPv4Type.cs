using System.Text.RegularExpressions;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The `IPv4` scalar type represents a valid a IPv4 address as defined
    /// here https://en.wikipedia.org/wiki/IPv4.
    /// </summary>
    public class IPv4Type : RegularExpressionType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IPv4Type"/> class.
        /// </summary>
        public IPv4Type()
            : base(
                WellKnownScalarTypes.IPv4,
                ScalarResources.IPv4Type_ValidationPattern,
                ScalarResources.IPv4Type_Description,
                RegexOptions.Compiled | RegexOptions.IgnoreCase)
        {
        }
    }
}
