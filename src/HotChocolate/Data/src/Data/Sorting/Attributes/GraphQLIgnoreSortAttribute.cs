using System;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// A marker attribute that tells the Hot Chocolate engine to ignore the specified type
/// or property when generating <see cref="SortInputType{T}"/> objects.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public sealed class GraphQLIgnoreSortAttribute : Attribute
{
}
