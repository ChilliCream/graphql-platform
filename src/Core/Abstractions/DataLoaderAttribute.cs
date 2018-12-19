using System;

namespace HotChocolate
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class DataLoaderAttribute
        : Attribute
    {
    }
}
