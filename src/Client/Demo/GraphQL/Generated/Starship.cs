using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GraphQL
{
    public class Starship
        : ISearchResult
        , IStarship
    {
        public Starship(
            string? name)
        {
            Name = name;
        }

        public string? Name { get; }
    }
}
