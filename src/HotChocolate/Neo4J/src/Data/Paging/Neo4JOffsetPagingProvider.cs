using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Data.Neo4J.Execution;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.Neo4J.Paging
{
/// <summary>
    /// An offset paging provider for Neo4J that create pagination queries
    /// </summary>
    public class Neo4JOffsetPagingProvider : OffsetPagingProvider
    {
        public override bool CanHandle(IExtendedType source)
        {
            throw new NotImplementedException();
        }

        protected override OffsetPagingHandler CreateHandler(IExtendedType source, PagingOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
