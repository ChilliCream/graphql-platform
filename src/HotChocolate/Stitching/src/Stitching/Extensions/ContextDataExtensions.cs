using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Stitching.Merge;
using HotChocolate.Stitching.Merge.Rewriters;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Stitching.ThrowHelper;
using static HotChocolate.Stitching.WellKnownContextData;

namespace HotChocolate.Stitching;

internal static class ContextDataExtensions
{
    public static IReadOnlyDictionary<string, IRequestExecutor> GetRemoteExecutors(
        this IHasContextData hasContextData)
    {
        if (hasContextData.ContextData.TryGetValue(RemoteExecutors, out var o) &&
            o is IReadOnlyDictionary<string, IRequestExecutor> executors)
        {
            return executors;
        }

        throw RequestExecutorBuilder_RemoteExecutorNotFound();
    }

    public static IReadOnlyDictionary<string, IRequestExecutor> GetRemoteExecutors(
        this ISchema schema)
    {
        if (schema.ContextData.TryGetValue(RemoteExecutors, out var o) &&
            o is IReadOnlyDictionary<string, IRequestExecutor> executors)
        {
            return executors;
        }

        throw RequestExecutorBuilder_RemoteExecutorNotFound();
    }

    public static ISchemaBuilder AddRemoteExecutor(
        this ISchemaBuilder schemaBuilder,
        string schemaName,
        IRequestExecutor executor)
    {
        return schemaBuilder
            .SetContextData(
                RemoteExecutors,
                current =>
                {
                    if (current is not OrderedDictionary<string, IRequestExecutor> dict)
                    {
                        dict = new OrderedDictionary<string, IRequestExecutor>();
                    }

                    dict[schemaName] = executor;
                    return dict;
                });
    }

    public static IReadOnlyList<MergeTypeRuleFactory> GetTypeMergeRules(
        this IDescriptorContext hasContextData)
    {
        if (hasContextData.ContextData.TryGetValue(TypeMergeRules, out var o) &&
            o is IReadOnlyList<MergeTypeRuleFactory> rules)
        {
            return rules;
        }

        return Array.Empty<MergeTypeRuleFactory>();
    }

    public static ISchemaBuilder AddTypeMergeRules(
        this ISchemaBuilder schemaBuilder,
        MergeTypeRuleFactory mergeRuleFactory) =>
        schemaBuilder.SetContextData(
            TypeMergeRules,
            current =>
            {
                if (current is not List<MergeTypeRuleFactory> list)
                {
                    list = new List<MergeTypeRuleFactory>();
                }

                list.Add(mergeRuleFactory);
                return list;
            });

    public static IReadOnlyList<MergeDirectiveRuleFactory> GetDirectiveMergeRules(
        this IDescriptorContext hasContextData)
    {
        if (hasContextData.ContextData.TryGetValue(DirectiveMergeRules, out var o) &&
            o is IReadOnlyList<MergeDirectiveRuleFactory> rules)
        {
            return rules;
        }

        return Array.Empty<MergeDirectiveRuleFactory>();
    }

    public static ISchemaBuilder AddDirectiveMergeRules(
        this ISchemaBuilder schemaBuilder,
        MergeDirectiveRuleFactory mergeRuleFactory) =>
        schemaBuilder.SetContextData(
            DirectiveMergeRules,
            current =>
            {
                if (!(current is List<MergeDirectiveRuleFactory> list))
                {
                    list = new List<MergeDirectiveRuleFactory>();
                }

                list.Add(mergeRuleFactory);
                return list;
            });

    public static IReadOnlyList<ITypeRewriter> GetTypeRewriter(
        this IDescriptorContext hasContextData)
    {
        if (hasContextData.ContextData.TryGetValue(TypeRewriter, out var o) &&
            o is IReadOnlyList<ITypeRewriter> rules)
        {
            return rules;
        }

        return Array.Empty<ITypeRewriter>();
    }

    public static ISchemaBuilder AddTypeRewriter(
        this ISchemaBuilder schemaBuilder,
        ITypeRewriter rewriter) =>
        schemaBuilder.SetContextData(
            TypeRewriter,
            current =>
            {
                if (!(current is List<ITypeRewriter> list))
                {
                    list = new List<ITypeRewriter>();
                }

                list.Add(rewriter);
                return list;
            });

    public static IReadOnlyList<IDocumentRewriter> GetDocumentRewriter(
        this IDescriptorContext hasContextData)
    {
        if (hasContextData.ContextData.TryGetValue(DocumentRewriter, out var o) &&
            o is IReadOnlyList<IDocumentRewriter> rules)
        {
            return rules;
        }

        return Array.Empty<IDocumentRewriter>();
    }

    public static ISchemaBuilder AddDocumentRewriter(
        this ISchemaBuilder schemaBuilder,
        IDocumentRewriter rewriter) =>
        schemaBuilder.SetContextData(
            DocumentRewriter,
            current =>
            {
                if (!(current is List<IDocumentRewriter> list))
                {
                    list = new List<IDocumentRewriter>();
                }

                list.Add(rewriter);
                return list;
            });

    public static IReadOnlyList<DocumentNode> GetTypeExtensions(
        this IDescriptorContext hasContextData,
        string? name = null)
    {
        var key = name is not null ? $"{TypeExtensions}.{name}" : TypeExtensions;

        if (hasContextData.ContextData.TryGetValue(key, out var o) &&
            o is IReadOnlyList<DocumentNode> rules)
        {
            return rules;
        }

        return Array.Empty<DocumentNode>();
    }

    public static ISchemaBuilder AddTypeExtensions(
        this ISchemaBuilder schemaBuilder,
        DocumentNode document,
        string? name = null)
    {
        var key = name is not null ? $"{TypeExtensions}.{name}" : TypeExtensions;

        return schemaBuilder.SetContextData(
            key,
            current =>
            {
                if (!(current is List<DocumentNode> list))
                {
                    list = new List<DocumentNode>();
                }

                list.Add(document);
                return list;
            });
    }

    public static IReadOnlyList<Func<DocumentNode, DocumentNode>> GetMergedDocRewriter(
        this IDescriptorContext hasContextData)
    {
        if (hasContextData.ContextData.TryGetValue(MergedDocRewriter, out var o) &&
            o is IReadOnlyList<Func<DocumentNode, DocumentNode>> rules)
        {
            return rules;
        }

        return Array.Empty<Func<DocumentNode, DocumentNode>>();
    }

    public static ISchemaBuilder AddMergedDocRewriter(
        this ISchemaBuilder schemaBuilder,
        Func<DocumentNode, DocumentNode> rewrite) =>
        schemaBuilder.SetContextData(
            MergedDocRewriter,
            current =>
            {
                if (!(current is List<Func<DocumentNode, DocumentNode>> list))
                {
                    list = new List<Func<DocumentNode, DocumentNode>>();
                }

                list.Add(rewrite);
                return list;
            });

    public static IReadOnlyList<Action<DocumentNode>> GetMergedDocVisitors(
        this IDescriptorContext hasContextData)
    {
        if (hasContextData.ContextData.TryGetValue(MergedDocVisitors, out var o) &&
            o is IReadOnlyList<Action<DocumentNode>> rules)
        {
            return rules;
        }

        return Array.Empty<Action<DocumentNode>>();
    }

    public static ISchemaBuilder AddMergedDocVisitor(
        this ISchemaBuilder schemaBuilder,
        Action<DocumentNode> visit) =>
        schemaBuilder.SetContextData(
            MergedDocVisitors,
            current =>
            {
                if (!(current is List<Action<DocumentNode>> list))
                {
                    list = new List<Action<DocumentNode>>();
                }

                list.Add(visit);
                return list;
            });

    public static IReadOnlyDictionary<string, ISet<string>> GetExternalFieldLookup(
        this IHasContextData hasContextData)
    {
        if (hasContextData.ContextData.TryGetValue(ExternalFieldLookup, out var value) &&
            value is IReadOnlyDictionary<string, ISet<string>> dict)
        {
            return dict;
        }

        return new Dictionary<string, ISet<string>>();
    }

    public static ISchemaBuilder AddExternalFieldLookup(
        this ISchemaBuilder schemaBuilder,
        IReadOnlyDictionary<string, ISet<string>> externalFieldLookup)
    {
        return schemaBuilder.SetContextData(ExternalFieldLookup, externalFieldLookup);
    }

    public static IReadOnlyDictionary<(string, string), string> GetNameLookup(
        this ISchema schema)
    {
        if (schema.ContextData.TryGetValue(NameLookup, out var value) &&
            value is IReadOnlyDictionary<(string, string), string> dict)
        {
            return dict;
        }

        throw RequestExecutorBuilder_NameLookupNotFound();
    }

    public static IReadOnlyDictionary<(string, string), string> GetNameLookup(
        this IHasContextData hasContextData)
    {
        if (hasContextData.ContextData.TryGetValue(NameLookup, out var value) &&
            value is IReadOnlyDictionary<(string, string), string> dict)
        {
            return dict;
        }

        return new Dictionary<(string, string), string>();
    }

    public static ISchemaBuilder AddNameLookup(
        this ISchemaBuilder schemaBuilder,
        IReadOnlyDictionary<(string, string), string> nameLookup)
    {
        return schemaBuilder.SetContextData(NameLookup, current =>
        {
            if (current is IDictionary<(string, string), string> dict)
            {
                foreach (var item in nameLookup)
                {
                    dict[item.Key] = item.Value;
                }

                return dict;
            }

            return nameLookup.ToDictionary(t => t.Key, t => t.Value);
        });
    }

    public static ISchemaBuilder AddNameLookup(
        this ISchemaBuilder schemaBuilder,
        string originalTypeName,
        string newTypeName,
        string schemaName)
    {
        return schemaBuilder.SetContextData(NameLookup, current =>
        {
            if (current is IDictionary<(string, string), string> dict)
            {
                dict[(newTypeName, schemaName)] = originalTypeName;

                return dict;
            }

            return new Dictionary<(string, string), string>
            {
                { (newTypeName, schemaName), originalTypeName }
            };
        });
    }

    public static IReadOnlyList<RemoteSchemaDefinition> GetSchemaDefinitions(
        this IReadOnlyDictionary<string, object?> contextData)
    {
        if (contextData.TryGetValue(WellKnownContextData.SchemaDefinitions, out var o) &&
            o is IReadOnlyList<RemoteSchemaDefinition> schemaDefinitions)
        {
            return schemaDefinitions;
        }

        return Array.Empty<RemoteSchemaDefinition>();
    }

    public static List<RemoteSchemaDefinition> GetOrAddSchemaDefinitions(
        this IDescriptorContext descriptorContext)
    {
        if (descriptorContext.ContextData.TryGetValue(
                WellKnownContextData.SchemaDefinitions,
                out var o) &&
            o is List<RemoteSchemaDefinition> schemaDefinitions)
        {
            return schemaDefinitions;
        }

        schemaDefinitions = new List<RemoteSchemaDefinition>();
        descriptorContext.ContextData.Add(
            WellKnownContextData.SchemaDefinitions,
            schemaDefinitions);
        return schemaDefinitions;
    }
}
