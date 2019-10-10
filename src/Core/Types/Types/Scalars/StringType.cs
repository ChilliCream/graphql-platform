using System;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    /// <summary>
    /// The String scalar type represents textual data, represented as
    /// UTF‐8 character sequences. The String type is most often used
    /// by GraphQL to represent free‐form human‐readable text.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-String
    /// </summary>
    [SpecScalar]
    public sealed class StringType
        : ScalarType<string, StringValueNode>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringType"/> class.
        /// </summary>
        public StringType()
            : base(ScalarNames.String)
        {
            Description = TypeResources.StringType_Description;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringType"/> class.
        /// </summary>
        public StringType(NameString name)
            : base(name)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringType"/> class.
        /// </summary>
        public StringType(NameString name, string description)
            : base(name)
        {
            Description = description;
        }

        protected override string ParseLiteral(StringValueNode literal)
        {
            return literal.Value;
        }

        protected override StringValueNode ParseValue(string value)
        {
            return new StringValueNode(value);
        }
    }
}
