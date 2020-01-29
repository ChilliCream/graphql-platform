using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.StarWarsAll
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
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
