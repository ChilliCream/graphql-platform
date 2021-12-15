using System;

namespace HotChocolate;

/// <summary>
/// Specifies that a resolver parameter represents the parent object.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ParentAttribute : Attribute
{
}
