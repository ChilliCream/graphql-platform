using System;
using System.Collections.Generic;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J.Language
{
    public class PageRequest
    {
        /**
         * Start from 1
         */
        public int Page { get; }

        /**
         * Start from 1
         */
        public int Size { get; }

        public OrderBy Order { get; set; }

        public PageRequest(int page, int size)
        {
            if (page < 1 || size < 1)
            {
                throw new Neo4jException("Illegal page or size, they must be greater than 1");
            }

            Page = page;
            Size = size;
        }

        public long Offset()
        {
            return (Page - 1) * (long) Size;
        }
    }

    public class Page<T>
    {
        public int CurrentPage { get; }
        public int PageSize { get; }
        public IList<T> Items { get; }

        public int TotalPages => Items.Count == 0 ? 1 : (int) Math.Ceiling(TotalItems / (double) PageSize);
        public long TotalItems { get; }

        public Page(PageRequest request)
        {
            CurrentPage = request.Page;
            PageSize = request.Size;
        }
    }
}
