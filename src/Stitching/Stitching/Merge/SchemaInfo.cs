using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;
using HotChocolate.Language;
using System;

namespace HotChocolate.Stitching.Merge
{
    internal class SchemaInfo
        : ISchemaInfo
    {
        public SchemaInfo(string name, DocumentNode document)
        {
            if (string.IsNullOrEmpty(name))
            {
                // TODO : resources
                throw new ArgumentException(
                    "The schema name mustn't be null or empty.",
                    nameof(name));
            }

            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            Name = name;
            Document = document;

            Dictionary<string, ITypeDefinitionNode> types =
                document.Definitions
                    .OfType<ITypeDefinitionNode>()
                    .ToDictionary(t => t.Name.Value);
            Types = types;

            Directives = document.Definitions
                .OfType<DirectiveDefinitionNode>()
                .ToDictionary(t => t.Name.Value);

            SchemaDefinitionNode schemaDefinition = document.Definitions
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

        public NameString Name { get; }

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

        public bool TryGetOperationType(
            ObjectTypeDefinitionNode rootType,
            out OperationType operationType)
        {
            if (rootType == QueryType)
            {
                operationType = OperationType.Query;
                return true;
            }

            if (rootType == MutationType)
            {
                operationType = OperationType.Mutation;
                return true;
            }

            if (rootType == SubscriptionType)
            {
                operationType = OperationType.Subscription;
                return true;
            }

            operationType = default;
            return false;
        }

        private static ObjectTypeDefinitionNode ResolveRootType(
            IDictionary<string, ITypeDefinitionNode> types,
            SchemaDefinitionNodeBase schemaDefinition,
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
