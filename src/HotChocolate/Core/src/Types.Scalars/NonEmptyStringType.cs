using System;
using HotChocolate.Language;
using HotChocolate.Types.Scalars;

namespace HotChocolate.Types
{
    /// <summary>
    /// The NonNullString scalar type represents non empty textual data, represented as
    /// UTF‐8 character sequences with at least one character
    /// </summary>
    public class NonEmptyStringType : StringType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NonEmptyStringType "/> class.
        /// </summary>
        public NonEmptyStringType()
            : this(
                WellKnownScalarTypes.NonNullString,
                ScalarResources.NonNullStringType_Description)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NonEmptyStringType "/> class.
        /// </summary>
        public NonEmptyStringType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, description, bind)
        {
            Description = description;
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(string runtimeValue)
        {
            return runtimeValue != string.Empty;
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(StringValueNode valueSyntax)
        {
            return valueSyntax.Value != string.Empty;
        }

        /// <inheritdoc />
        protected override string ParseLiteral(StringValueNode valueSyntax)
        {
            if (valueSyntax.Value == string.Empty)
            {
                throw ThrowHelper.NonNullStringType_ParseLiteral_IsEmpty(this);
            }

            return base.ParseLiteral(valueSyntax);
        }

        /// <inheritdoc />
        protected override StringValueNode ParseValue(string runtimeValue)
        {
            if (runtimeValue == string.Empty)
            {
                throw ThrowHelper.NonNullStringType_ParseValue_IsEmpty(this);
            }

            return base.ParseValue(runtimeValue);
        }
    }
}
