using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// The Boolean scalar type represents true or false.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Boolean
    /// </summary>
    [SpecScalar]
    public sealed class BooleanType
        : ScalarType<bool, BooleanValueNode>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BooleanType"/> class.
        /// </summary>
        public BooleanType()
            : base(ScalarNames.Boolean, BindingBehavior.Implicit)
        {
            Description = TypeResources.BooleanType_Description;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BooleanType"/> class.
        /// </summary>
        public BooleanType(NameString name)
            : base(name, BindingBehavior.Implicit)
        {
            Description = TypeResources.BooleanType_Description;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BooleanType"/> class.
        /// </summary>
        public BooleanType(NameString name, string description)
            : base(name, BindingBehavior.Implicit)
        {
            Description = description;
        }

        protected override bool ParseLiteral(BooleanValueNode valueSyntax)
        {
            return valueSyntax.Value;
        }

        protected override BooleanValueNode ParseValue(bool runtimeValue)
        {
            return runtimeValue ? BooleanValueNode.True : BooleanValueNode.False;
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            if (resultValue is null)
            {
                return NullValueNode.Default;
            }

            if (resultValue is bool b)
            {
                return b ? BooleanValueNode.True : BooleanValueNode.False;
            }

            throw new SerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseResult(Name, resultValue.GetType()),
                this);
        }
    }
}
