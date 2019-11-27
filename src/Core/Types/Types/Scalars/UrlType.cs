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
            : base(ScalarNames.Url)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlType"/> class.
        /// </summary>
        public UrlType(NameString name)
            : base(name)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlType"/> class.
        /// </summary>
        public UrlType(NameString name, string description)
            : base(name)
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

        public override bool TryDeserialize(object serialized, out object value)
        {
            if (serialized is null)
            {
                value = null;
                return true;
            }

            if (serialized is string s && TryParseUri(s, out Uri uri))
            {
                value = uri;
                return true;
            }

            if (serialized is Uri u)
            {
                value = u;
                return true;
            }

            value = null;
            return false;
        }

        private bool TryParseUri(string value, out Uri uri) =>
            Uri.TryCreate(value, UriKind.Absolute, out uri);
    }
}
