using System;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J
{
    /*public abstract class Repository<T>
    {
        public abstract Task<EntityId> CreateAsync(IEntityCreate<T> update);

        Task<IEnumerable<T>> CreateAsync(IEnumerable<IEntityCreate<T>> updates);

        Task<T> UpdateAsync(IEntityUpdate<T> update);

        Task<IEnumerable<T>> UpdateAsync(IEnumerable<IEntityUpdate<T>> updates);

        Task<bool> DeleteByIdAsync(EntityId id);

        Task<bool> DeleteByIdsAsync(IEnumerable<EntityId> ids);

        Task<T> GetByIdAsync(EntityId id);

        Task<IEnumerable<T>> GetByIdsAsync(IEnumerable<EntityId> ids);
    }*/

    public class Neo4JRepository
    {
        /// <summary>
        /// Neo4J Driver
        /// </summary>
        private readonly IDriver _driver;

        /// <summary>
        /// Configuration of Neo4J session builder
        /// </summary>
        private readonly Action<SessionConfigBuilder> _sessionBuilder;

        public Neo4JRepository(IDriver driver, string database)
        {
            _driver = driver;
            _sessionBuilder = o => o.WithDatabase(database);
        }
    }
}
