using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Stitching.Merge;
using HotChocolate.Stitching.Merge.Rewriters;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Stitching.WellKnownContextData;

namespace HotChocolate.Stitching
{
    internal static class ContextDataExtensions
    {
        public static IReadOnlyDictionary<NameString, IRequestExecutor> GetRemoteExecutors(
           this IHasContextData hasContextData)
        {
            if (hasContextData.ContextData.TryGetValue(RemoteExecutors, out object? o) &&
                o is IReadOnlyDictionary<NameString, IRequestExecutor> executors)
            {
                return executors;
            }

            // TODO : throw helper
            throw new InvalidOperationException(
                "The mandatory remote executors have not been found.");
        }

        public static IReadOnlyDictionary<NameString, IRequestExecutor> GetRemoteExecutors(
            this ISchema schema)
        {
            if (schema.ContextData.TryGetValue(RemoteExecutors, out object? o) &&
                o is IReadOnlyDictionary<NameString, IRequestExecutor> executors)
            {
                return executors;
            }

            // TODO : throw helper
            throw new InvalidOperationException(
                "The mandatory remote executors have not been found.");
        }

        public static ISchemaBuilder AddRemoteExecutor(
            this ISchemaBuilder schemaBuilder,
            NameString schemaName,
            IRequestExecutor executor)
        {
            return schemaBuilder
                .SetContextData(
                    RemoteExecutors,
                    current =>
                    {
                        if (!(current is OrderedDictionary<NameString, IRequestExecutor> dict))
                        {
                            dict = new OrderedDictionary<NameString, IRequestExecutor>();
                        }

                        dict[schemaName] = executor;
                        return dict;
                    });
        }

        public static IReadOnlyList<MergeTypeRuleFactory> GetTypeMergeRules(
            this IDescriptorContext hasContextData)
        {
            if (hasContextData.ContextData.TryGetValue(TypeMergeRules, out object? o) &&
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
                    if (!(current is List<MergeTypeRuleFactory> list))
                    {
                        list = new List<MergeTypeRuleFactory>();
                    }

                    list.Add(mergeRuleFactory);
                    return list;
                });

        public static IReadOnlyList<MergeDirectiveRuleFactory> GetDirectiveMergeRules(
            this IDescriptorContext hasContextData)
        {
            if (hasContextData.ContextData.TryGetValue(DirectiveMergeRules, out object? o) &&
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
            if (hasContextData.ContextData.TryGetValue(TypeRewriter, out object? o) &&
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
            if (hasContextData.ContextData.TryGetValue(DocumentRewriter, out object? o) &&
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
            string key = name is not null ? $"{TypeExtensions}.{name}" : TypeExtensions;

            if (hasContextData.ContextData.TryGetValue(key, out object? o) &&
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
            string key = name is not null ? $"{TypeExtensions}.{name}" : TypeExtensions;

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
            if (hasContextData.ContextData.TryGetValue(MergedDocRewriter, out object? o) &&
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
            if (hasContextData.ContextData.TryGetValue(MergedDocVisitors, out object? o) &&
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

        public static IReadOnlyDictionary<NameString, ISet<NameString>> GetExternalFieldLookup(
            this IHasContextData hasContextData)
        {
            if (hasContextData.ContextData.TryGetValue(ExternalFieldLookup, out object? value) &&
                value is IReadOnlyDictionary<NameString, ISet<NameString>> dict)
            {
                return dict;
            }

            return new Dictionary<NameString, ISet<NameString>>();
        }

        public static ISchemaBuilder AddExternalFieldLookup(
            this ISchemaBuilder schemaBuilder,
            IReadOnlyDictionary<NameString, ISet<NameString>> externalFieldLookup)
        {
            return schemaBuilder.SetContextData(ExternalFieldLookup, externalFieldLookup);
        }

        public static IReadOnlyDictionary<(NameString, NameString), NameString> GetNameLookup(
            this ISchema schema)
        {
            if (schema.ContextData.TryGetValue(NameLookup, out object? value) &&
                value is IReadOnlyDictionary<(NameString, NameString), NameString> dict)
            {
                return dict;
            }

            // todo . throw helper
            throw new InvalidOperationException("A stitched schema must provide a name lookup");
        }

        public static IReadOnlyDictionary<(NameString, NameString), NameString> GetNameLookup(
            this IHasContextData hasContextData)
        {
            if (hasContextData.ContextData.TryGetValue(NameLookup, out object? value) &&
                value is IReadOnlyDictionary<(NameString, NameString), NameString> dict)
            {
                return dict;
            }

            return new Dictionary<(NameString, NameString), NameString>();
        }

        public static ISchemaBuilder AddNameLookup(
            this ISchemaBuilder schemaBuilder,
            IReadOnlyDictionary<(NameString, NameString), NameString> nameLookup)
        {
            return schemaBuilder.SetContextData(NameLookup, current =>
            {
                if (current is IDictionary<(NameString, NameString), NameString> dict)
                {
                    foreach (KeyValuePair<(NameString, NameString), NameString> item in nameLookup)
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
            NameString originalTypeName,
            NameString newTypeName,
            NameString schemaName)
        {
            return schemaBuilder.SetContextData(NameLookup, current =>
            {
                if (current is IDictionary<(NameString, NameString), NameString> dict)
                {
                    dict[(newTypeName, schemaName)] = originalTypeName;

                    return dict;
                }

                return new Dictionary<(NameString, NameString), NameString>
                {
                    { (newTypeName, schemaName), originalTypeName }
                };
            });
        }

        public static IReadOnlyList<RemoteSchemaDefinition> GetSchemaDefinitions(
            this IReadOnlyDictionary<string, object?> contextData)
        {
            if (contextData.TryGetValue(WellKnownContextData.SchemaDefinitions, out object? o) &&
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
                out object? o) &&
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
}
