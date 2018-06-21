using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public sealed class StringType
        : StringTypeBase
    {
        public StringType()
            : base("String")
        {
        }
    }
}
