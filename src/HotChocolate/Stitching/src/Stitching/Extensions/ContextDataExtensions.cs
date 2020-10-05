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

        public static void AddRemoteExecutor(
            this IHasContextData hasContextData,
            NameString schemaName,
            IRequestExecutor executor) =>
            hasContextData.ContextData.AddRemoteExecutor(schemaName, executor);

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
    }
}
