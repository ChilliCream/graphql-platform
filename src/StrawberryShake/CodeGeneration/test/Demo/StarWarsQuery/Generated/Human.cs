using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.StarWarsQuery
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class Human
        : IHuman
    {
        public Human(
            double? height, 
            string? name, 
            global::StrawberryShake.Client.StarWarsQuery.IFriend? friends)
        {
            Height = height;
            Name = name;
            Friends = friends;
        }

        public double? Height { get; }

        public string? Name { get; }

        public global::StrawberryShake.Client.StarWarsQuery.IFriend? Friends { get; }
    }
}
