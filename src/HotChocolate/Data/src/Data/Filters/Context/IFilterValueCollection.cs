using System.Collections.Generic;

namespace HotChocolate.Data.Filters;

/// <summary>
/// Represents a collection of <see cref="IFilterValueNode"/>
/// </summary>
public interface IFilterValueCollection : IEnumerable<IFilterValueNode>, IFilterValueNode
{
}
