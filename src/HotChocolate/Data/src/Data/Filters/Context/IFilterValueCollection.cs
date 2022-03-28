using System.Collections.Generic;

namespace HotChocolate.Data.Filters;

/// <summary>
/// Represents a collection of <see cref="IFilterValueInfo">
/// </summary>
public interface IFilterValueCollection : IEnumerable<IFilterValueInfo>, IFilterValueInfo
{
}
