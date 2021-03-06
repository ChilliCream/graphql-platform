using System;

namespace HotChocolate
{
    /// <summary>
    /// Resolver arguments annotated with this attribute that
    /// are objects will be spread into multiple arguments in
    /// the resulting schema, one for each of the object's members
    /// as if member was on the resolver directly. At execution
    /// time, the resolver method will still however receive the
    /// object.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class SpreadAttribute
        : Attribute
    {
    }
}
