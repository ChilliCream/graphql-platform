using System;
using System.Collections.Generic;
using StrawberryShake;

namespace Foo
{
    public class Droid
        : IDroid
    {
        public string Name { get; set; }
        public IFriend Friends { get; set; }
    }}
