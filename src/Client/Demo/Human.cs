using System;
using System.Collections.Generic;
using StrawberryShake;

namespace Foo
{
    public class Human
        : IHuman
    {
        public string Name { get; set; }
        public IFriend Friends { get; set; }
    }
}
