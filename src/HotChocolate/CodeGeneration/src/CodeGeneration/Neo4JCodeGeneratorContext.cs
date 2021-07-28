using System;
using System.Collections.Generic;
using HotChocolate.Language;

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
        /// GraphQL documents to generate source for.
        /// </summary>
        public IReadOnlyList<DocumentNode> Documents { get; }

        public Neo4JCodeGeneratorContext(
            string name,
            string databaseName,
            string @namespace,
            IReadOnlyList<DocumentNode> documents)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            DatabaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
            Namespace = @namespace ?? throw new ArgumentNullException(nameof(@namespace));
            Documents = documents;
        }
    }
}
