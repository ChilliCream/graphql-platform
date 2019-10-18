using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    /// <summary>
    /// The name scalar represents a valid GraphQL name as specified in the spec
    /// and can be used to refer to fields or types.
    /// </summary>
    public sealed class NameType
        : ScalarType<NameString, StringValueNode>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NameType"/> class.
        /// </summary>
        public NameType()
            : base(ScalarNames.Name)
        {
            Description = TypeResources.NameType_Description;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameType"/> class.
        /// </summary>
        public NameType(NameString name)
            : base(name)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameType"/> class.
        /// </summary>
        public NameType(NameString name, string description)
            : base(name)
        {
            Description = description;
        }

        protected override bool IsInstanceOfType(StringValueNode literal)
        {
            return NameUtils.IsValidGraphQLName(literal.AsSpan());
        }

        protected override NameString ParseLiteral(StringValueNode literal)
        {
            if (IsInstanceOfType(literal))
            {
                return new NameString(literal.Value);
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()));
        }

        protected override StringValueNode ParseValue(NameString value)
        {
            return new StringValueNode(value.Value);
        }

        public override bool TrySerialize(object value, out object serialized)
        {
            if (value is null)
            {
                serialized = null;
                return true;
            }

            if (value is NameString name)
            {
                serialized = name.Value;
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

            if (serialized is string s)
            {
                value = new NameString(s);
                return true;
            }

            if (serialized is NameString n)
            {
                value = n;
                return true;
            }

            value = null;
            return false;
        }
    }
}
