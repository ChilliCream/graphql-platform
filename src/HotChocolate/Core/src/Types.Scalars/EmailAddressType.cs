using HotChocolate.Language;
using HotChocolate.Types.Scalars;

namespace HotChocolate.Types
{
    /// <summary>
    /// A field whose value conforms to the standard internet email address format as specified in HTML Spec
    /// </summary>
    public class EmailAddressType : StringType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmailAddressType"/> class.
        /// </summary>

        public EmailAddressType()
            : this(
                WellKnownScalarTypes.EmaillAddress,
                ScalarResources.EmailAddressType_Description)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailAddressType"/> class.
        /// </summary>
        public EmailAddressType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, description, bind)
        {
            Description = description;
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(string runtimeValue)
        {
            return runtimeValue !== string.Empty;
        }
    }
}