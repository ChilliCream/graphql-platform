using System;
using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    public class TypeInfo
        : ITypeInfo
    {
        public TypeInfo(
            ITypeDefinitionNode definition,
            DocumentNode schema,
            string schemaName)
        {
            if (string.IsNullOrEmpty(schemaName))
            {
                throw new ArgumentException(
                    "The schema name mustn't be null or empty.",
                    nameof(schemaName));
            }

            Definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
            Schema = schema
                ?? throw new ArgumentNullException(nameof(schema));
            SchemaName = schemaName;
        }

        public ITypeDefinitionNode Definition { get; }

        public DocumentNode Schema { get; }

        public string SchemaName { get; }
    }
}
