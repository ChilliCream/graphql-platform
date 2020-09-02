using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

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

        protected override bool IsInstanceOfType(StringValueNode valueSyntax)
        {
            return MultiplierPathString.IsValidPath(valueSyntax.AsSpan());
        }

        protected override MultiplierPathString ParseLiteral(StringValueNode valueSyntax)
        {
            if (IsInstanceOfType(valueSyntax))
            {
                return new MultiplierPathString(valueSyntax.Value);
            }

            throw new SerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(Name, valueSyntax.GetType()),
                this);
        }

        protected override StringValueNode ParseValue(MultiplierPathString runtimeValue)
        {
            return new StringValueNode(runtimeValue.Value);
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            if (resultValue is null)
            {
                return NullValueNode.Default;
            }

            if (resultValue is string s)
            {
                return new StringValueNode(s);
            }

            if (resultValue is MultiplierPathString p)
            {
                return new StringValueNode(p.Value);
            }

            throw new SerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseResult(Name, resultValue.GetType()),
                this);
        }

        public override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            if (runtimeValue is null)
            {
                resultValue = null;
                return true;
            }

            if (runtimeValue is MultiplierPathString path)
            {
                resultValue = path.Value;
                return true;
            }

            resultValue = null;
            return false;
        }

        public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
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
