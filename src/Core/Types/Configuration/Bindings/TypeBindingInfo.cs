using System;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal class TypeBindingInfo
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public BindingBehavior Behavior { get; set; }
        public List<FieldBindingInfo> Fields { get; } =
            new List<FieldBindingInfo>();
    }
}
