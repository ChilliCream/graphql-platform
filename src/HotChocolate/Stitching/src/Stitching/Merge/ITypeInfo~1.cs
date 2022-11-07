using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge
{
    public interface ITypeInfo<out T>
        : ITypeInfo
        where T : ITypeDefinitionNode
    {
        new T Definition { get; }
    }
}
