using System;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    public sealed class UrlType
        : ScalarType<Uri, StringValueNode>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UrlType"/> class.
        /// </summary>
        public UrlType()
            : base(ScalarNames.Url, BindingBehavior.Implicit)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlType"/> class.
        /// </summary>
        public UrlType(NameString name)
            : base(name, BindingBehavior.Implicit)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlType"/> class.
        /// </summary>
        public UrlType(NameString name, string description)
            : base(name, BindingBehavior.Implicit)
        {
            Description = description;
        }

        protected override bool IsInstanceOfType(StringValueNode literal)
        {
            return TryParseUri(literal.Value, out _);
        }

        protected override Uri ParseLiteral(StringValueNode literal)
        {
            if (TryParseUri(literal.Value, out Uri uri))
            {
                return uri;
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()));
        }

        protected override StringValueNode ParseValue(Uri value)
        {
            return new StringValueNode(value.AbsoluteUri);
        }

        public override bool TrySerialize(object value, out object serialized)
        {
            if (value is null)
            {
                serialized = null;
                return true;
            }

            if (value is Uri uri)
            {
                serialized = uri.AbsoluteUri;
                return true;
            }

            serialized = null;
            return false;
        }

        public override bool TryDeserialize(object resultValue, out object runtimeValue)
        {
            if (resultValue is null)
            {
                runtimeValue = null;
                return true;
            }

            if (resultValue is string s && TryParseUri(s, out Uri uri))
            {
                runtimeValue = uri;
                return true;
            }

            if (resultValue is Uri u)
            {
                runtimeValue = u;
                return true;
            }

            runtimeValue = null;
            return false;
        }

        private bool TryParseUri(string value, out Uri uri) =>
            Uri.TryCreate(value, UriKind.Absolute, out uri);
    }
}
