using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client
{
    public class HasName
        : IHasName
    {
        public HasName(
            string? name)
        {
            Name = name;
        }

        public string? Name { get; }
    }
}
