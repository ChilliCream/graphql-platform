using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.StarWarsAll
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
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
