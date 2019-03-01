using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;
using HotChocolate.Language;
using System;
using HotChocolate.Stitching.Properties;

namespace HotChocolate.Stitching.Merge
{
    internal class SchemaInfo
        : ISchemaInfo
    {
        private static readonly Dictionary<OperationType, string> _names =
            new Dictionary<OperationType, string>
            {
                {
                    OperationType.Query,
                    OperationType.Query.ToString()
                },
                {
                    OperationType.Mutation,
                    OperationType.Mutation.ToString()
                },
                {
                    OperationType.Subscription,
                    OperationType.Subscription.ToString()
                }
            };
        private ObjectTypeDefinitionNode _queryType;
        private ObjectTypeDefinitionNode _mutationType;
        private ObjectTypeDefinitionNode _subscriptionType;

        public SchemaInfo(string name, DocumentNode document)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    StitchingResources.SchemaName_EmptyOrNull,
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

            RootTypes = GetRootTypeMapppings(
                GetRootTypeNameMapppings(schemaDefinition),
                types);
        }

        protected Dictionary<OperationType, ObjectTypeDefinitionNode> RootTypes
        { get; }

        public NameString Name { get; }

        public DocumentNode Document { get; }

        public IReadOnlyDictionary<string, ITypeDefinitionNode> Types
        { get; }

        public IReadOnlyDictionary<string, DirectiveDefinitionNode> Directives
        { get; }

        public ObjectTypeDefinitionNode QueryType
        {
            get
            {
                if (_queryType == null
                    && RootTypes.TryGetValue(OperationType.Query,
                        out ObjectTypeDefinitionNode type))
                {
                    _queryType = type;
                }
                return _queryType;
            }
        }

        public ObjectTypeDefinitionNode MutationType
        {
            get
            {
                if (_mutationType == null
                    && RootTypes.TryGetValue(OperationType.Mutation,
                        out ObjectTypeDefinitionNode type))
                {
                    _mutationType = type;
                }
                return _mutationType;
            }
        }

        public ObjectTypeDefinitionNode SubscriptionType
        {
            get
            {
                if (_subscriptionType == null
                    && RootTypes.TryGetValue(OperationType.Subscription,
                        out ObjectTypeDefinitionNode type))
                {
                    _subscriptionType = type;
                }
                return _subscriptionType;
            }
        }

        public bool IsRootType(ITypeDefinitionNode typeDefinition)
        {
            if (typeDefinition == null)
            {
                throw new ArgumentNullException(nameof(typeDefinition));
            }

            if (typeDefinition is ObjectTypeDefinitionNode ot)
            {
                return RootTypes.ContainsValue(ot);
            }

            return false;
        }

        public bool TryGetOperationType(
            ObjectTypeDefinitionNode rootType,
            out OperationType operationType)
        {
            if (RootTypes.ContainsValue(rootType))
            {
                operationType = RootTypes.First(t => t.Value == rootType).Key;
                return true;
            }

            operationType = default;
            return false;
        }

        private static Dictionary<OperationType, ObjectTypeDefinitionNode>
            GetRootTypeMapppings(
                IDictionary<OperationType, string> nameMappings,
                IDictionary<string, ITypeDefinitionNode> types)
        {
            var map = new Dictionary<OperationType, ObjectTypeDefinitionNode>();

            foreach (KeyValuePair<OperationType, string> nameMapping in
                nameMappings)
            {
                if (types.TryGetValue(nameMapping.Value, out
                    ITypeDefinitionNode definition)
                    && definition is ObjectTypeDefinitionNode objectType)
                {
                    types.Remove(nameMapping.Value);
                    map.Add(nameMapping.Key, objectType);
                }
            }

            return map;
        }

        private static IDictionary<OperationType, string>
            GetRootTypeNameMapppings(SchemaDefinitionNodeBase schemaDefinition)
        {
            if (schemaDefinition == null)
            {
                return _names;
            }

            return schemaDefinition.OperationTypes.ToDictionary(
                t => t.Operation,
                t => t.Type.Name.Value);
        }
    }
}
