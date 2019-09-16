using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client
{
    public class Droid0
        : IDroid0
    {
        public Droid0(
            string? name)
        {
            name = Name;
        }
        public string? Name { get; }
    }
}
