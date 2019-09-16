using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client
{
    public class Droid1
        : IDroid1
    {
        public Droid1(
            string? name)
        {
            name = Name;
        }
        public string? Name { get; }
    }
}
