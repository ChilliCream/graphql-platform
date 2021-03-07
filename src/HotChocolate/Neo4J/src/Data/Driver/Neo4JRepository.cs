using System;
using System.Collections.Generic;
using HotChocolate.Data.Neo4J.Language;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J
{
    public class Neo4JRepository
    {
        private readonly IDriver _driver;

        private readonly Action<SessionConfigBuilder> _sessionBuilder;

        private readonly Neo4JContext _context;

        public Neo4JRepository(IDriver driver, string database, Neo4JContext context)
        {
            _driver = driver;
            _context = context;
            _sessionBuilder = o => o.WithDatabase(database);
        }

        private IAsyncSession NewSession()
        {
            return _driver.AsyncSession(_sessionBuilder);
        }

        public T Create<T>(T entity)
        {

            return entity;
        }

        public T Update<T>(T entity)
        {

            return entity;
        }

        public void DeleteById<T>(long id, bool isDetached = false)
        {

        }

        public void DeleteAll<T>()
        {

        }
    }
}
