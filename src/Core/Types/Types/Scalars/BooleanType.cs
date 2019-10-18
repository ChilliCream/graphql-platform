using System;
using HotChocolate.Language;
using HotChocolate.Properties;

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
            : base(ScalarNames.Boolean)
        {
            Description = TypeResources.BooleanType_Description;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BooleanType"/> class.
        /// </summary>
        public BooleanType(NameString name)
            : base(name)
        {
            Description = TypeResources.BooleanType_Description;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BooleanType"/> class.
        /// </summary>
        public BooleanType(NameString name, string description)
            : base(name)
        {
            Description = description;
        }

        protected override bool ParseLiteral(BooleanValueNode literal)
        {
            return literal.Value;
        }

        protected override BooleanValueNode ParseValue(bool value)
        {
            return value ? BooleanValueNode.TrueLiteral : BooleanValueNode.FalseLiteral;
        }
    }
}
