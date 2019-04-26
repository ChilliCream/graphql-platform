using System;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    /// <summary>
    /// The `Upload` scalar type represents a file upload.
    ///
    /// https://github.com/jaydenseric/graphql-multipart-request-spec
    /// </summary>
    [SpecScalar]
    public sealed class UploadType : ScalarType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UploadType"/> class.
        /// </summary>
        public UploadType() : base("Upload")
        {
            Description = TypeResources.UploadType_Description;
        }

        public override Type ClrType => typeof(Upload);
        public override bool IsInstanceOfType(IValueNode literal)
        {
            throw new NotSupportedException("`Upload` scalar literal unsupported.");
        }

        public override object ParseLiteral(IValueNode literal)
        {
            throw new NotSupportedException("`Upload` scalar literal unsupported.");
        }

        public override IValueNode ParseValue(object value)
        {
            throw new NotSupportedException("`Upload` scalar value unsupported.");
        }

        public override object Serialize(object value)
        {
            throw new NotSupportedException("`Upload` scalar serialization unsupported.");
        }

        public override bool TryDeserialize(object serialized, out object value)
        {
            switch (serialized)
            {
                case null:
                    value = null;
                    return true;
                case Upload u:
                    value = u;
                    return true;
                default:
                    value = null;
                    return false;
            }
        }
    }
}
