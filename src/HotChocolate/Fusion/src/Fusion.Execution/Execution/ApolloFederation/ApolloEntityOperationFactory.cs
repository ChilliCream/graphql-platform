using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Metadata;

namespace HotChocolate.Fusion.Execution.ApolloFederation;

internal sealed class ApolloEntityOperationFactory
{
    private const string ApolloFederationKind = "ApolloFederation";

    private readonly FusionSchemaDefinition _schema;
    private readonly Dictionary<string, FederationQueryRewriter> _rewriters = [with(StringComparer.Ordinal)];

    public ApolloEntityOperationFactory(FusionSchemaDefinition schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        _schema = schema;
    }

    public bool TryCreateEntityOperation(
        string? schemaName,
        OperationSourceText operation,
        out ApolloEntityOperation? entityOperation)
    {
        entityOperation = null;

        if (schemaName is null || !IsApolloFederationSchema(schemaName))
        {
            return false;
        }

        var rewritten = GetRewriter(schemaName).GetOrRewrite(
            operation.SourceText,
            operation.SourceText.ComputeHash());

        if (!rewritten.IsEntityLookup)
        {
            return false;
        }

        var representationFields = rewritten.VariableToKeyFieldMap
            .Select(static t => new ApolloRepresentationField(t.Key, t.Value))
            .ToArray();

        entityOperation = new ApolloEntityOperation(
            OperationSourceText.Create(operation.Name, operation.Type, rewritten.OperationText),
            rewritten.EntityTypeName!,
            rewritten.LookupFieldName!,
            rewritten.InlineFragment?.ToString(indented: true) ?? string.Empty,
            representationFields);

        return true;
    }

    private bool IsApolloFederationSchema(string schemaName)
    {
        var kind = _schema.GetSourceSchemaConnectorKind(schemaName);

        return string.Equals(kind, ApolloFederationKind, StringComparison.Ordinal);
    }

    private FederationQueryRewriter GetRewriter(string schemaName)
    {
        if (!_rewriters.TryGetValue(schemaName, out var rewriter))
        {
            rewriter = CreateRewriter(schemaName);
            _rewriters.Add(schemaName, rewriter);
        }

        return rewriter;
    }

    private FederationQueryRewriter CreateRewriter(string schemaName)
    {
        var lookups = new Dictionary<string, LookupFieldInfo>(StringComparer.Ordinal);
        var entityRequires = new Dictionary<string, EntityRequiresInfo>(StringComparer.Ordinal);

        foreach (var type in _schema.Types.AsEnumerable(allowInaccessibleFields: true))
        {
            if (type is not FusionObjectTypeDefinition objectType)
            {
                continue;
            }

            if (objectType.Sources.TryGetMember(schemaName, out var sourceObjectType))
            {
                ProjectLookups(sourceObjectType, lookups);
            }

            ProjectEntityRequires(schemaName, objectType, entityRequires);
        }

        return new FederationQueryRewriter(lookups, entityRequires);
    }

    private static void ProjectLookups(
        SourceObjectType sourceObjectType,
        Dictionary<string, LookupFieldInfo> lookups)
    {
        foreach (var lookup in sourceObjectType.Lookups)
        {
            var argumentToKeyFieldMap = new Dictionary<string, string>(StringComparer.Ordinal);

            for (var i = 0; i < lookup.Arguments.Length; i++)
            {
                var argument = lookup.Arguments[i];
                var selection = lookup.Fields[i];
                argumentToKeyFieldMap[argument.Name] = LookupArgumentPathMapper.Map(selection);
            }

            lookups[lookup.FieldName] = new LookupFieldInfo
            {
                EntityTypeName = lookup.FieldType.Name,
                ArgumentToKeyFieldMap = argumentToKeyFieldMap
            };
        }
    }

    private static void ProjectEntityRequires(
        string schemaName,
        FusionObjectTypeDefinition objectType,
        Dictionary<string, EntityRequiresInfo> entityRequires)
    {
        Dictionary<string, IReadOnlyDictionary<string, string>>? fieldMap = null;

        foreach (var field in objectType.Fields.AsEnumerable(allowInaccessibleFields: true))
        {
            if (!field.Sources.TryGetMember(schemaName, out var sourceField)
                || sourceField.Requirements is null)
            {
                continue;
            }

            var requirements = sourceField.Requirements;
            var requires = new Dictionary<string, string>(StringComparer.Ordinal);

            for (var i = 0; i < requirements.Arguments.Length; i++)
            {
                var argument = requirements.Arguments[i];
                var selection = requirements.Fields[i];

                if (selection is null)
                {
                    continue;
                }

                var path = LookupArgumentPathMapper.Map(selection);

                if (path.Length == 0)
                {
                    path = argument.Name;
                }

                requires[argument.Name] = path;
            }

            if (requires.Count > 0)
            {
                fieldMap ??= new Dictionary<string, IReadOnlyDictionary<string, string>>(
                    StringComparer.Ordinal);
                fieldMap[field.Name] = requires;
            }
        }

        if (fieldMap is { Count: > 0 })
        {
            entityRequires[objectType.Name] = new EntityRequiresInfo
            {
                Fields = fieldMap
            };
        }
    }
}
