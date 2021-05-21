using System;
using System.Collections;

namespace HotChocolate.Data.Neo4J
{
    internal interface ICollectionMapper
    {
        IEnumerable MapValues(IEnumerable fromList, Type toInstanceOfType);
    }
}
