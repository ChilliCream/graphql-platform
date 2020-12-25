using System.Collections.Generic;

namespace StrawberryShake
{
    /// <summary>
    /// A result data info exposes all information necessary to
    /// rebuild the result data object by using entity ids.
    /// </summary>
    public interface IOperationResultDataInfo
    {
        /// <summary>
        /// A read-only set (reflexive and transitive closure) of all entities that are used by the operation result.
        /// This includes all entities (And not only the entities on the root level of the result).
        /// </summary>
        IReadOnlyCollection<EntityId> EntityIds { get; }
    }
}
