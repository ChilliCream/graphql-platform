using System;
using System.Collections.Generic;

namespace HotChocolate.Configuration
{
    public class TypeBindingInfo
    {
        public Type Type { get; set; }
        public string Name { get; set; }
        public BindingBehavior Behavior { get; set; }
        public List<FieldBindingInfo> Fields { get; } =
            new List<FieldBindingInfo>();
    }
}
