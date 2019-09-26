using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client
{
    public class Hero
        : IHero
    {
        public Hero(
            double? height, 
            string? name, 
            IFriend? friends)
        {
            Height = height;
            Name = name;
            Friends = friends;
        }

        public double? Height { get; }

        public string? Name { get; }

        public IFriend? Friends { get; }
    }
}
