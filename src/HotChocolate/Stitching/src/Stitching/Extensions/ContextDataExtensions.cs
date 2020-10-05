using System;
using System.Collections.Generic;
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

        public static void AddRemoteExecutor(
            this IDictionary<string, object?> contextData,
            NameString schemaName,
            IRequestExecutor executor)
        {
            if (!contextData.ContainsKey(RemoteExecutors))
            {
                contextData.Add(
                    RemoteExecutors,
                    new OrderedDictionary<NameString, IRequestExecutor>());
            }

            if (contextData.TryGetValue(RemoteExecutors, out object? o) &&
                o is IDictionary<NameString, IRequestExecutor> executors)
            {
                executors[schemaName] = executor;
            }
            else
            {
                // TODO : throw helper
                throw new InvalidOperationException(
                    "The mandatory remote executors have not been found.");
            }
        }

        public static ISchemaBuilder AddRemoteExecutor(
            this ISchemaBuilder schemaBuilder,
            NameString schemaName,
            IRequestExecutor executor)
        {
            return schemaBuilder
                .SetSchema(descriptor => descriptor
                    .Extend()
                    .OnBeforeCreate(def =>
                        def.ContextData.AddRemoteExecutor(schemaName, executor)))
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
            this IDescriptorContext hasContextData)
        {
            if (hasContextData.ContextData.TryGetValue(TypeExtensions, out object? o) &&
                o is IReadOnlyList<DocumentNode> rules)
            {
                return rules;
            }

            return Array.Empty<DocumentNode>();
        }

        public static ISchemaBuilder AddTypeExtensions(
            this ISchemaBuilder schemaBuilder,
            DocumentNode document) =>
            schemaBuilder.SetContextData(
                TypeExtensions,
                current =>
                {
                    if (!(current is List<DocumentNode> list))
                    {
                        list = new List<DocumentNode>();
                    }

                    list.Add(document);
                    return list;
                });

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
            if (hasContextData.ContextData.TryGetValue(MergedDocRewriter, out object? o) &&
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
                MergedDocRewriter,
                current =>
                {
                    if (!(current is List<Action<DocumentNode>> list))
                    {
                        list = new List<Action<DocumentNode>>();
                    }

                    list.Add(visit);
                    return list;
                });
    }
}
