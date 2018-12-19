using System;

namespace HotChocolate.Types
{
    /// <summary>
    /// Defines that the annotated type is an internal intospection type.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class,
        Inherited = false,
        AllowMultiple = false)]
    internal sealed class IntrospectionAttribute
        : Attribute
    {
    }
}
