using System.Buffers.Text;
using System;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    public sealed class UuidType
        : ScalarType<Guid, StringValueNode>
    {
        private const char _format = 'N';

        /// <summary>
        /// Initializes a new instance of the <see cref="UuidType"/> class.
        /// </summary>
        public UuidType()
            : base(ScalarNames.Uuid)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UuidType"/> class.
        /// </summary>
        public UuidType(NameString name)
            : base(name)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UuidType"/> class.
        /// </summary>
        public UuidType(NameString name, string description)
            : base(name)
        {
            Description = description;
        }

        protected override bool IsInstanceOfType(StringValueNode literal)
        {
            return Utf8Parser.TryParse(literal.AsSpan(), out Guid _, out int _, _format);
        }

        protected override Guid ParseLiteral(StringValueNode literal)
        {
            if (Utf8Parser.TryParse(literal.AsSpan(), out Guid g, out int _, _format))
            {
                return g;
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()));
        }

        protected override StringValueNode ParseValue(Guid value)
        {
            return new StringValueNode(value.ToString("N"));
        }

        public override bool TrySerialize(object value, out object serialized)
        {
            if (value is null)
            {
                serialized = null;
                return true;
            }

            if (value is Guid uri)
            {
                serialized = uri.ToString("N");
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

            if (serialized is string s && Guid.TryParse(s, out Guid guid))
            {
                value = guid;
                return true;
            }

            if (serialized is Guid)
            {
                value = serialized;
                return true;
            }

            value = null;
            return false;
        }
    }
}
