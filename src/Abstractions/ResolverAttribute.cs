using System;

namespace HotChocolate
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class ResolverAttribute
        : Attribute
    {
    }
}
