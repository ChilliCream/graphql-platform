using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Projections
{
    /// <summary>
    /// This class was ported over from the official mongo db driver
    /// </summary>
    internal sealed class Neo4JCombinedProjectionDefinition : Neo4JProjectionDefinition
    {
        private readonly Neo4JProjectionDefinition[] _projections;

        public Neo4JCombinedProjectionDefinition(
            params Neo4JProjectionDefinition[] projections)
        {
            _projections = projections;
        }

        public Neo4JCombinedProjectionDefinition(
            IEnumerable<Neo4JProjectionDefinition> projections)
        {
            ///_projections = Ensure.IsNotNull(projections, nameof(projections)).ToArray();
        }

    }
}
