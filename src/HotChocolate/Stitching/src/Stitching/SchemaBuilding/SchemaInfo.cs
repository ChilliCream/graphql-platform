using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Stitching.Properties;
using static HotChocolate.Language.OperationType;

namespace HotChocolate.Stitching.SchemaBuilding;

internal class SchemaInfo : ISchemaInfo
{
    private static readonly Dictionary<OperationType, string> _names =
        new()
        {
            { Query, nameof(Query) },
            { Mutation, nameof(Mutation) },
            { Subscription, nameof(Subscription) }
        };
    private ObjectTypeDefinitionNode? _queryType;
    private ObjectTypeDefinitionNode? _mutationType;
    private ObjectTypeDefinitionNode? _subscriptionType;

    public SchemaInfo(string name, DocumentNode document)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException(
                StitchingResources.SchemaName_EmptyOrNull,
                nameof(name));
        }

        Name = name;
        Document = document ?? throw new ArgumentNullException(nameof(document));

        var types =
            document.Definitions
                .OfType<ITypeDefinitionNode>()
                .ToDictionary(t => t.Name.Value);
        Types = types;

        Directives = document.Definitions
            .OfType<DirectiveDefinitionNode>()
            .ToDictionary(t => t.Name.Value);

        SchemaDefinitionNode? schemaDefinition = document.Definitions
            .OfType<SchemaDefinitionNode>().FirstOrDefault();

        RootTypes = GetRootTypeMappings(
            GetRootTypeNameMapppings(schemaDefinition),
            types);
    }

    protected Dictionary<OperationType, ObjectTypeDefinitionNode> RootTypes { get; }

    public NameString Name { get; }

    public DocumentNode Document { get; }

    public IReadOnlyDictionary<string, ITypeDefinitionNode> Types { get; }

    public IReadOnlyDictionary<string, DirectiveDefinitionNode> Directives { get; }

    public ObjectTypeDefinitionNode QueryType
    {
        get
        {
            if (_queryType == null &&
                RootTypes.TryGetValue(Query, out ObjectTypeDefinitionNode? type))
            {
                _queryType = type;
            }
            return _queryType;
        }
    }

    public ObjectTypeDefinitionNode? MutationType
    {
        get
        {
            if (_mutationType == null &&
                RootTypes.TryGetValue(Mutation, out ObjectTypeDefinitionNode? type))
            {
                _mutationType = type;
            }
            return _mutationType;
        }
    }

    public ObjectTypeDefinitionNode? SubscriptionType
    {
        get
        {
            if (_subscriptionType == null &&
                RootTypes.TryGetValue(Subscription, out ObjectTypeDefinitionNode? type))
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

    private static Dictionary<OperationType, ObjectTypeDefinitionNode> GetRootTypeMappings(
        IDictionary<OperationType, string> nameMappings,
        IDictionary<string, ITypeDefinitionNode> types)
    {
        var map = new Dictionary<OperationType, ObjectTypeDefinitionNode>();

        foreach (KeyValuePair<OperationType, string> nameMapping in nameMappings)
        {
            if (types.TryGetValue(nameMapping.Value, out ITypeDefinitionNode? definition) &&
                definition is ObjectTypeDefinitionNode objectType)
            {
                types.Remove(nameMapping.Value);
                map.Add(nameMapping.Key, objectType);
            }
        }

        return map;
    }

    private static IDictionary<OperationType, string> GetRootTypeNameMapppings(
        SchemaDefinitionNodeBase? schemaDefinition)
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
