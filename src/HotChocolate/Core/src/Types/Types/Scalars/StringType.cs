using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

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
            : this(
                ScalarNames.String,
                TypeResources.StringType_Description,
                BindingBehavior.Implicit)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringType"/> class.
        /// </summary>
        public StringType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, bind)
        {
            Description = description;
        }

        protected override string ParseLiteral(StringValueNode valueSyntax) =>
            valueSyntax.Value;

        protected override StringValueNode ParseValue(string runtimeValue) =>
            new(runtimeValue);

        public override IValueNode ParseResult(object? resultValue) =>
            ParseValue(resultValue);
    }
}
