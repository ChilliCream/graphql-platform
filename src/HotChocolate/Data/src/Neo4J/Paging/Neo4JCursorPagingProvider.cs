using System;
using HotChocolate.Internal;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.Neo4J.Paging
{
public class Neo4JCursorPagingProvider : CursorPagingProvider
    {
        public override bool CanHandle(IExtendedType source)
        {
            throw new NotImplementedException();
        }

        protected override CursorPagingHandler CreateHandler(IExtendedType source, PagingOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
