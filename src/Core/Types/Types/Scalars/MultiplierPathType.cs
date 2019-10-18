using System;
using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    /// <summary>
    /// The name scalar represents a valid GraphQL name as specified in the spec
    /// and can be used to refer to fields or types.
    /// </summary>
    public sealed class MultiplierPathType
        : ScalarType<MultiplierPathString, StringValueNode>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MultiplierPathType"/> class.
        /// </summary>
        public MultiplierPathType()
            : base(ScalarNames.MultiplierPath)
        {
            Description = TypeResources.MultiplierPathType_Description;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiplierPathType"/> class.
        /// </summary>
        public MultiplierPathType(NameString name)
            : base(name)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiplierPathType"/> class.
        /// </summary>
        public MultiplierPathType(NameString name, string description)
            : base(name)
        {
            Description = description;
        }

        protected override bool IsInstanceOfType(StringValueNode literal)
        {
            return MultiplierPathString.IsValidPath(literal.AsSpan());
        }

        protected override MultiplierPathString ParseLiteral(StringValueNode literal)
        {
            if (IsInstanceOfType(literal))
            {
                return new MultiplierPathString(literal.Value);
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()));
        }

        protected override StringValueNode ParseValue(MultiplierPathString value)
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

            if (value is MultiplierPathString path)
            {
                serialized = path.Value;
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
                value = new MultiplierPathString(s);
                return true;
            }

            if (serialized is MultiplierPathString p)
            {
                value = p;
                return true;
            }

            value = null;
            return false;
        }
    }
}
