using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public sealed class IdType
        : StringTypeBase
    {
        public IdType()
            : base("ID")
        {
        }
    }
}
