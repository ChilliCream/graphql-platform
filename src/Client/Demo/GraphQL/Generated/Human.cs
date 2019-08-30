using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace Foo
{
    public class Human
        : IHuman
    {
        public double? Height { get; set; }

        public string Name { get; set; }

        public IFriend Friends { get; set; }
    }
}
