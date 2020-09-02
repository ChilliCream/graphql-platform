using HotChocolate.Internal;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    /// <summary>
    /// Specifies a filter input type.
    /// </summary>
    public interface IFilterInputType
        : IInputObjectType
    {
        IExtendedType EntityType { get; }
    }
}
