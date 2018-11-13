using System;
using HotChocolate.Language;

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

            return literal is StringValueNode
                || literal is NullValueNode;
        }

        public override object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is StringValueNode stringLiteral
                && TryParseUri(stringLiteral.Value, out Uri uri))
            {
                return uri;
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new ArgumentException(
                TypeResources.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()),
                nameof(literal));
        }

        public override IValueNode ParseValue(object value)
        {
            if (value == null)
            {
                return new NullValueNode(null);
            }

            if (value is Uri uri)
            {
                return new StringValueNode(Serialize(uri));
            }

            throw new ArgumentException(
                TypeResources.Scalar_Cannot_ParseValue(
                    Name, value.GetType()),
                nameof(value));
        }

        public override object Serialize(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is Uri uri)
            {
                return Serialize(uri);
            }

            throw new ArgumentException(
                TypeResources.Scalar_Cannot_Serialize(Name));
        }

        private string Serialize(Uri value)
        {
            return value.AbsoluteUri;
        }

        public override object Deserialize(object value)
        {
            if (value is null)
            {
                return null;
            }

            if (value is string s && TryParseUri(s, out Uri uri))
            {
                return uri;
            }

            throw new ArgumentException(
                TypeResources.Scalar_Cannot_Serialize(Name));
        }

        private bool TryParseUri(string value, out Uri uri) =>
            Uri.TryCreate(value, UriKind.Absolute, out uri);
    }
}
