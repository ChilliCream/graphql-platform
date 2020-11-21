using System;

namespace HotChocolate
{
    /// <summary>
    /// Indicates to tooling that this method builds a Schema using an ISchemaBuilder
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SchemaBuilderMethodAttribute : Attribute
    {
    }
}
