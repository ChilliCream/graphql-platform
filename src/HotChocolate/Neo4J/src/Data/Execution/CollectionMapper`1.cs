using System;
using System.Collections;
using System.Collections.Generic;
using ServiceStack.Text;

namespace HotChocolate.Data.Neo4J
{
    internal class CollectionMapper<TResult> : ICollectionMapper
    {
        public IEnumerable MapValues(IEnumerable fromList, Type toInstanceOfType)
        {
            var to = (ICollection<TResult>)TranslateListWithElements<TResult>
                .CreateInstance(toInstanceOfType);

            foreach (object item in fromList)
            {
                to.Add(ValueMapper.MapValue<TResult>(item));
            }

            return to;
        }
    }
}
