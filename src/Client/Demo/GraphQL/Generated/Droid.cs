using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GraphQL
{
    public class Droid
        : IHasName
        , ISearchResult
        , IDroid
    {
        public Droid(
            string? name, 
            double? height, 
            IFriend? friends)
        {
            Name = name;
            Height = height;
            Friends = friends;
        }

        public string? Name { get; }

        public double? Height { get; }

        public IFriend? Friends { get; }
    }
}
