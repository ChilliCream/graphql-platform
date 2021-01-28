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

        /// <inheritdoc />
        protected override bool IsInstanceOfType(StringValueNode valueSyntax)
        {
            return valueSyntax.Value !== string.Empty;
        }

        /// <inheritdoc />
        protected override string ParseLiteral(StringValueNode valueSyntax)
        {
            Regex rgx = new Regex(@"/^\+[1-9]\d{1,14}$/",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var match = rgx.Match(valueSyntax);
            
            if(!match)
            {
                throw ThrowHelper.EmailAddressType_ParseLiteral_IsEmpty(this);
            }

            return base.ParseLiteral(valueSyntax);
        }

        /// <inheritdoc />
        protected override StringValueNode PareseValue(string runtimeValue)
        {
            Regex rgx = new Regex(@"/^\+[1-9]\d{1,14}$/",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var match = rgx.Match(runtimeValue);
            
            if(!match)
            {
                throw ThrowHelper.EmailAddressType_ParseValue_IsEmpty(this);
            }

            return base.ParseValue(runtimeValue);
        }

        /// <inheritdoc />
        public override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            Regex rgx = new Regex(@"/^\+[1-9]\d{1,14}$/",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var match = rgx.Match(runtimeValue);
            
            if(runtimeValue is string s && !match)
            {
                resultValue = null;
                return false;
            }

            return base.TrySerialize(runtimeValue, out resultValue);
        }

        /// <inheritdoc />
        public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
        {
            Regex rgx = new Regex(@"/^\+[1-9]\d{1,14}$/",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var match = rgx.Match(runtimeValue);
            
            if (!base.TryDeserialize(resultValue, out runtimeValue))
            {
                return false;
            }
            
            if(runtimeValue is string s && !match)
            {
                return false;
            }

            return true;
        }
    }
}