using HotChocolate.Language;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.Types
{
    /// <summary>
    /// The GraphQL Upload scalar.
    /// </summary>
    public class UploadType : ScalarType<IFormFile>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UploadType"/> class.
        /// </summary>
        public UploadType() : this("Upload")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadType"/> class.
        /// </summary>
        public UploadType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, bind)
        {
            Description = description;
        }

        public override object? ParseLiteral(IValueNode valueSyntax, bool withDefaults = true)
        {
            throw new GraphQLException("Upload literal unsupported.");
        }

        public override bool IsInstanceOfType(IValueNode valueSyntax)
        {
            throw new GraphQLException("Upload value invalid.");
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            throw new GraphQLException("Upload value invalid.");
        }

        public override IValueNode ParseValue(object? runtimeValue)
        {
            throw new GraphQLException("Upload value invalid.");
        }

        public override object? Serialize(object? runtimeValue)
        {
            throw new GraphQLException("Upload serialization unsupported.");
        }
    }
}
