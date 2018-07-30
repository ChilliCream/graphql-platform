using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    /// <summary>
    /// The String scalar type represents textual data, represented as
    /// UTF‐8 character sequences. The String type is most often used by GraphQL
    /// to represent free‐form human‐readable text.
    ///
    /// All response formats must support string representations,
    /// and that representation must be used here.
    /// </summary>
    public sealed class StringType
        : StringTypeBase
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="StringType"/> class.
        /// </summary>
        public StringType()
            : base("String")
        {
        }
    }
}
