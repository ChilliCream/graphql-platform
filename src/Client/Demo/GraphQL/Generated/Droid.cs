using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client
{
    public class Droid
        : IDroid
    {
        public Droid(
            double? height, 
            string? name, 
            IFriend0 friends)
        {
            height = Height;
            name = Name;
            friends = Friends;
        }
        public double? Height { get; }

        public string? Name { get; }

        public IFriend0 Friends { get; }
    }
}
