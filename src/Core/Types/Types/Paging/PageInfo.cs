using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Paging
{
    public class PageInfo
        : IPageInfo
    {
        public PageInfo(
            bool hasNextPage, bool hasPreviousPage,
            string startCursor, string endCursor)
        {
            HasNextPage = hasNextPage;
            HasPreviousPage = hasPreviousPage;
            StartCursor = startCursor;
            EndCursor = endCursor;
        }

        public bool HasNextPage { get; }

        public bool HasPreviousPage { get; }

        public string StartCursor { get; }

        public string EndCursor { get; }
    }
}
