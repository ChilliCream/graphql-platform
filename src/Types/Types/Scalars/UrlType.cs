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
                && TryParseLiteral(stringLiteral, out object obj))
            {
                return obj;
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new ArgumentException(
                $"The {Name} can only parse string literals.",
                nameof(literal));
        }

        private bool TryParseLiteral(StringValueNode literal, out object obj)
        {
            if (Uri.TryCreate(literal.Value, UriKind.Absolute, out Uri uri))
            {
                obj = uri;
                return true;
            }

            obj = null;
            return false;
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
                $"The specified value has to be a valid {Name} " +
                $"in order to be parsed by the {Name}.");
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
                $"The specified value cannot be serialized by the {Name}.");
        }

        private string Serialize(Uri value)
        {
            return value.AbsoluteUri;
        }
    }
}
