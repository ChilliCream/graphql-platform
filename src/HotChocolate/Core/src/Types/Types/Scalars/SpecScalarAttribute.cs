using System;

namespace HotChocolate.Types
{
    /// <summary>
    /// Defines that the annotated scalar type is defined
    /// in the GraphQL specification.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class,
        Inherited = false,
        AllowMultiple = false)]
    internal sealed class SpecScalarAttribute
        : Attribute
    {
    }
}
