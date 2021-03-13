using System;
using System.Collections.Generic;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Language;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J
{
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

        /// <summary>
        /// Neo4J Database context holding all node and relationship types and there properties
        /// </summary>
        private readonly Neo4JContext _context;

        public Neo4JRepository(IDriver driver, string database, Neo4JContext context)
        {
            _driver = driver;
            _context = context;
            _sessionBuilder = o => o.WithDatabase(database);
        }
        public T Create<T>(T entity)
        {
            Meta meta = _context.GetMetaData(entity.GetType());

            var properties = new List<object>();
            foreach (RegularProperty prop in meta.RegularProperties)
            {
                object value = prop.GetValue(entity);
                if (value == null) continue;

                properties.Push(prop.GetName());
                properties.Push( Cypher.LiteralOf(value));
            }

            Node node = Cypher.NamedNode(meta.Key).WithProperties(properties.ToArray());
            StatementBuilder statement = Cypher.Create(node);

           IAsyncSession session = _driver.AsyncSession(_sessionBuilder);
           session.WriteTransactionAsync(tx => tx.RunAsync(statement.Build()));

           return entity;
        }

        public List<T> CreateMany<T>(List<T> entities)
        {
            var nodes = new List<IPatternElement>();

            foreach (T entity in entities)
            {
                Meta meta = _context.GetMetaData(entity.GetType());
                var properties = new List<object>();
                foreach (RegularProperty prop in meta.RegularProperties)
                {
                    object value = prop.GetValue(entity);
                    if (value == null) continue;

                    properties.Push(prop.GetName());
                    properties.Push( Cypher.LiteralOf(value));
                }

                Node node = Cypher.NamedNode(meta.Key).WithProperties(properties.ToArray());
                nodes.Push(node);
            }

            StatementBuilder statement = Cypher.Create(nodes);

            IAsyncSession session = _driver.AsyncSession(_sessionBuilder);
            session.WriteTransactionAsync(tx => tx.RunAsync(statement.Build()));

            return entities;
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
