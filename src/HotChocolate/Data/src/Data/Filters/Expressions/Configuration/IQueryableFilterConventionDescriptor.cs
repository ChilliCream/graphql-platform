using System.Linq;
using HotChocolate.Data.Filters;

namespace HotChocolate.Data;

/// <summary>
/// A specific representation of <see cref="IFilterConventionDescriptor"/>
/// for filtering on <see cref="IQueryable"/>
/// </summary>
public interface IQueryableFilterConventionDescriptor : IFilterConventionDescriptor
{
}
