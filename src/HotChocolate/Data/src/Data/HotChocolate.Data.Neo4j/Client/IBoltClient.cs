using System;

namespace HotChocolate.Data.Neo4j
{
    public interface IBoltClient : IDisposable
    {
        T Add<T>(T entity) where T : EntityBase, new();
        void Connect();
        bool Ping();
    }
}
