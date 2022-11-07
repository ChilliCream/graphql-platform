using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge
{
    public interface ITypeInfo
    {
        ITypeDefinitionNode Definition { get; }

        ISchemaInfo Schema { get; }

        bool IsRootType { get; }
    }
}
