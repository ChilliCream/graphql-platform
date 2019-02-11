using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;
using HotChocolate.Language;
using System;

namespace HotChocolate.Stitching
{
    internal class SchemaInfo
        : ISchemaInfo
    {
        public SchemaInfo(string name, DocumentNode schema)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new System.ArgumentException(
                    "The schema name mustn't be null or empty.",
                    nameof(name));
            }

            Name = name;
            Document = schema
                ?? throw new ArgumentNullException(nameof(schema));

            Dictionary<string, ITypeDefinitionNode> types = schema.Definitions
                .OfType<ITypeDefinitionNode>()
                .ToDictionary(t => t.Name.Value);
            Types = types;

            Directives = schema.Definitions
                .OfType<DirectiveDefinitionNode>()
                .ToDictionary(t => t.Name.Value);

            SchemaDefinitionNode schemaDefinition = schema.Definitions
                .OfType<SchemaDefinitionNode>().FirstOrDefault();

            QueryType = ResolveRootType(
                types,
                schemaDefinition,
                OperationType.Query);

            MutationType = ResolveRootType(
                types,
                schemaDefinition,
                OperationType.Mutation);

            SubscriptionType = ResolveRootType(
                types,
                schemaDefinition,
                OperationType.Subscription);
        }

        public string Name { get; }

        public DocumentNode Document { get; }

        public IReadOnlyDictionary<string, ITypeDefinitionNode> Types
        { get; }

        public IReadOnlyDictionary<string, DirectiveDefinitionNode> Directives
        { get; }

        public ObjectTypeDefinitionNode QueryType { get; }

        public ObjectTypeDefinitionNode MutationType { get; }

        public ObjectTypeDefinitionNode SubscriptionType { get; }

        public bool IsRootType(ITypeDefinitionNode typeDefinition)
        {
            if (typeDefinition == null)
            {
                throw new ArgumentNullException(nameof(typeDefinition));
            }

            return typeDefinition == QueryType
                || typeDefinition == MutationType
                || typeDefinition == SubscriptionType;
        }

        private static ObjectTypeDefinitionNode ResolveRootType(
            IDictionary<string, ITypeDefinitionNode> types,
            SchemaDefinitionNode schemaDefinition,
            OperationType operation)
        {
            string typeName = null;

            if (schemaDefinition != null)
            {
                NamedTypeNode namedType = schemaDefinition.OperationTypes
                    .FirstOrDefault(t => t.Operation == OperationType.Query)?
                    .Type;
                typeName = namedType?.Name.Value;
            }

            typeName = operation.ToString();

            if (types.TryGetValue(typeName, out ITypeDefinitionNode definition)
                && definition is ObjectTypeDefinitionNode objectType)
            {
                types.Remove(typeName);
                return objectType;
            }

            return null;
        }
    }
}
