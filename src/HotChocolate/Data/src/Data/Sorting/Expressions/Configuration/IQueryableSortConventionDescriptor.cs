using System.Linq;

namespace HotChocolate.Data.Sorting.Expressions;

/// <summary>
/// A specific representation of <see cref="ISortConventionDescriptor"/>
/// for filtering on <see cref="IQueryable{T}"/>
/// </summary>
public interface IQueryableSortConventionDescriptor : ISortConventionDescriptor
{
}
