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
            string startToken, string endToken)
        {
            HasNextPage = hasNextPage;
            HasPreviousPage = hasPreviousPage;
            StartCursor = startToken;
            EndCursor = endToken;
        }

        public bool HasNextPage { get; }

        public bool HasPreviousPage { get; }

        public string StartCursor { get; }

        public string EndCursor { get; }
    }
}
