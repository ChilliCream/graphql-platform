using System;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    public sealed class UrlType
        : ScalarType
    {
        public UrlType()
            : base("Url")
        {
        }

        public override Type ClrType => typeof(Uri);

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is NullValueNode)
            {
                return true;
            }

            if (literal is StringValueNode stringLiteral
                && TryParseUri(stringLiteral.Value, out _))
            {
                return true;
            }

            return false;
        }

        public override object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            if (literal is StringValueNode stringLiteral
                && TryParseUri(stringLiteral.Value, out Uri uri))
            {
                return uri;
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()));
        }

        public override IValueNode ParseValue(object value)
        {
            if (value == null)
            {
                return new NullValueNode(null);
            }

            if (value is Uri uri)
            {
                return new StringValueNode(uri.AbsoluteUri);
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseValue(
                    Name, value.GetType()));
        }

        public override object Serialize(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is Uri uri)
            {
                return uri.ToString();
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_Serialize(Name));
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
