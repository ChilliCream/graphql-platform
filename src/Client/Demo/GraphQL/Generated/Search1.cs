using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GraphQL
{
    public class Search1
        : ISearch
    {
        public Search1(
            IReadOnlyList<ISearchResult>? search)
        {
            Search = search;
        }

        public IReadOnlyList<ISearchResult>? Search { get; }
    }
}
