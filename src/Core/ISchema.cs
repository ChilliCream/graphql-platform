using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate
{
    public interface ISchema
        : IEnumerable<IType>
    {
        IType GetType(string name);
        T GetType<T>(string name) where T : IType;
    }
}