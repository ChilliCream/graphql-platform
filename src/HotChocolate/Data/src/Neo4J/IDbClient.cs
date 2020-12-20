using System.Collections.Generic;
using HotChocolate.Data.Neo4J.Language;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J
{
    public interface IDbClient
    {
        public IDriver Driver { get; }
        bool IsConnected { get; }
        IReadOnlyList<T> GetByProperty<T>(string propertyName, object propertValue);
        IList<T> GetByProperties<T>(Dictionary<string, object> entity);
        T Add<T>(T entity);
        T Update<T>(T entity);
        T Delete<T>(string uuid);
        T SoftDelete<T>(string uuid);
        T GetByUuidWithRelatedNodes<T>(string uuid);
        IList<T> GetAll<T>(string where = default);
        bool CreateRelationship(string uuidFrom, string uuidTo, Dictionary<string, object> props = null);
        bool DeleteRelationship(string uuidIncoming, string uuidOutgoing);
        IList<T> RunCustomQuery<T>(
            string query,
            Dictionary<string, object> parameters);
        IResultSummary RunCustomQuery(
            string query,
            Dictionary<string, object> parameters = null);
        bool AddLabel(string uuid, string newLabelName);
    }
}
