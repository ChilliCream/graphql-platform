using System;

namespace HotChocolate
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class ParentAttribute
        : Attribute
    {
        public ParentAttribute()
        {

        }

        public ParentAttribute(string property)
        {
            Property = property;
        }

        public string Property { get; }
    }
}
