using System;
using System.Collections;

namespace HotChocolate.Data.Neo4J
{
    public interface ICollectionMapper
    {
        IEnumerable MapValues(IEnumerable fromList, Type toInstanceOfType);
    }
}
