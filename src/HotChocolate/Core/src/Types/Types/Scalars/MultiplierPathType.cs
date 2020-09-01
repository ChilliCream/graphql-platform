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
            : base(ScalarNames.MultiplierPath, BindingBehavior.Implicit)
        {
            Description = TypeResources.MultiplierPathType_Description;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiplierPathType"/> class.
        /// </summary>
        public MultiplierPathType(NameString name)
            : base(name, BindingBehavior.Implicit)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiplierPathType"/> class.
        /// </summary>
        public MultiplierPathType(NameString name, string description)
            : base(name, BindingBehavior.Implicit)
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

        public override bool TryDeserialize(object resultValue, out object runtimeValue)
        {
            if (resultValue is null)
            {
                runtimeValue = null;
                return true;
            }

            if (resultValue is string s)
            {
                runtimeValue = new MultiplierPathString(s);
                return true;
            }

            if (resultValue is MultiplierPathString p)
            {
                runtimeValue = p;
                return true;
            }

            runtimeValue = null;
            return false;
        }
    }
}
