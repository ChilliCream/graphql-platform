using System;

namespace HotChocolate.Data.Sorting;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class GraphQLIgnoreSortAttribute : Attribute
{
}
