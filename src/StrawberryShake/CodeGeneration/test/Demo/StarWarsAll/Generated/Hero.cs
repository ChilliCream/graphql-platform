using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.StarWarsAll
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public class Hero
        : IHero
    {
        public Hero(
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
