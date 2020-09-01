using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

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
            : base(ScalarNames.Name, BindingBehavior.Implicit)
        {
            Description = TypeResources.NameType_Description;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameType"/> class.
        /// </summary>
        public NameType(NameString name)
            : base(name, BindingBehavior.Implicit)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameType"/> class.
        /// </summary>
        public NameType(NameString name, string description)
            : base(name, BindingBehavior.Implicit)
        {
            Description = description;
        }

        protected override bool IsInstanceOfType(StringValueNode valueSyntax)
        {
            return NameUtils.IsValidGraphQLName(valueSyntax.AsSpan());
        }

        protected override NameString ParseLiteral(StringValueNode valueSyntax)
        {
            if (IsInstanceOfType(valueSyntax))
            {
                return new NameString(valueSyntax.Value);
            }

            throw new SerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(Name, valueSyntax.GetType()),
                this);
        }

        protected override StringValueNode ParseValue(NameString runtimeValue)
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

            if (resultValue is NameString n)
            {
                return n.HasValue
                    ? (IValueNode)new StringValueNode(n.Value)
                    : NullValueNode.Default;
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

            if (runtimeValue is NameString name)
            {
                resultValue = name.Value;
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
                runtimeValue = new NameString(s);
                return true;
            }

            if (resultValue is NameString n)
            {
                runtimeValue = n;
                return true;
            }

            runtimeValue = null;
            return false;
        }
    }
}
