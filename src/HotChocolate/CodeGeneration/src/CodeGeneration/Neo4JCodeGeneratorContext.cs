using System;

namespace HotChocolate.CodeGeneration
{
    public class Neo4JCodeGeneratorContext
    {
        /// <summary>
        /// A name for what's being generated.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Neo4J database name.
        /// </summary>
        public string DatabaseName { get; }

        /// <summary>
        /// The namespace to generate code under.
        /// </summary>
        public string Namespace { get; }

        /// <summary>
        /// A GraphQL schema to generate code for.
        /// </summary>
        public ISchema Schema { get; }

        public Neo4JCodeGeneratorContext(
            string name,
            string databaseName,
            string @namespace,
            ISchema schema)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            DatabaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
            Namespace = @namespace ?? throw new ArgumentNullException(nameof(@namespace));
            Schema = schema;
        }
    }
}
