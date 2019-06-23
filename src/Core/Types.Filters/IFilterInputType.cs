using System;

namespace HotChocolate.Types.Filters
{
    /// <summary>
    /// TODO: Cleanup, may omit.
    /// This interface is used for generic constraints
    /// </summary>
    public interface IFilterInputType
        : INamedInputType
    {
        Type EntityType { get; }
    }
}
