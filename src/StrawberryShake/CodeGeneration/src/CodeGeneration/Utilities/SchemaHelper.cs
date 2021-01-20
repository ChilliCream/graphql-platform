using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using StrawberryShake.CodeGeneration.Analyzers;

namespace StrawberryShake.CodeGeneration.Utilities
{
    public static class SchemaHelper
    {
        public static ISchema Load(params DocumentNode[] documents)
        {
            return Load((IEnumerable<DocumentNode>)documents);
        }

        public static ISchema Load(IEnumerable<DocumentNode> documents)
        {
            if (documents is null)
            {
                throw new ArgumentNullException(nameof(documents));
            }

            ISchemaBuilder builder = SchemaBuilder.New();
            var leafTypeInfos = new Dictionary<NameString, LeafTypeInfo>();
            var globalEntityPatterns = new List<SelectionSetNode>();
            var typeEntityPatterns = new Dictionary<NameString, SelectionSetNode>();

            foreach (DocumentNode document in documents)
            {
                if (document.Definitions.Any(t => t is ITypeSystemExtensionNode))
                {
                    CollectScalarInfos(
                        document.Definitions.OfType<ScalarTypeExtensionNode>(),
                        leafTypeInfos);

                    CollectEnumInfos(
                        document.Definitions.OfType<EnumTypeExtensionNode>(),
                        leafTypeInfos);

                    CollectGlobalEntityPatterns(
                        document.Definitions.OfType<SchemaExtensionNode>(),
                        globalEntityPatterns);

                    CollectTypeEntityPatterns(
                        document.Definitions.OfType<ObjectTypeExtensionNode>(),
                        typeEntityPatterns);
                }
                else
                {
                    builder.AddDocument(document);
                }
            }

            return builder
                .TryAddTypeInterceptor(
                    new LeafTypeInterceptor(leafTypeInfos))
                .TryAddTypeInterceptor(
                    new EntityTypeInterceptor(globalEntityPatterns, typeEntityPatterns))
                .Use(_ => _ => throw new NotSupportedException())
                .Create();
        }

        private static void CollectScalarInfos(
            IEnumerable<ScalarTypeExtensionNode> scalarTypeExtensions,
            Dictionary<NameString, LeafTypeInfo> leafTypeInfos)
        {
            foreach (ScalarTypeExtensionNode scalarTypeExtension in scalarTypeExtensions)
            {
                if (!leafTypeInfos.TryGetValue(
                    scalarTypeExtension.Name.Value,
                    out LeafTypeInfo scalarInfo))
                {
                    scalarInfo = new LeafTypeInfo(
                        scalarTypeExtension.Name.Value,
                        GetDirectiveValue(scalarTypeExtension, "runtimeType"),
                        GetDirectiveValue(scalarTypeExtension, "serializationType"));
                    leafTypeInfos.Add(scalarInfo.TypeName, scalarInfo);
                }
            }
        }

        private static void CollectEnumInfos(
            IEnumerable<EnumTypeExtensionNode> enumTypeExtensions,
            Dictionary<NameString, LeafTypeInfo> leafTypeInfos)
        {
            foreach (EnumTypeExtensionNode scalarTypeExtension in enumTypeExtensions)
            {
                if (!leafTypeInfos.TryGetValue(
                    scalarTypeExtension.Name.Value,
                    out LeafTypeInfo scalarInfo))
                {
                    scalarInfo = new LeafTypeInfo(
                        scalarTypeExtension.Name.Value,
                        GetDirectiveValue(scalarTypeExtension, "runtimeType"),
                        GetDirectiveValue(scalarTypeExtension, "serializationType"));
                    leafTypeInfos.Add(scalarInfo.TypeName, scalarInfo);
                }
            }
        }

        private static string? GetDirectiveValue(
            IHasDirectives hasDirectives,
            NameString directiveName)
        {
            DirectiveNode? directive = hasDirectives.Directives.FirstOrDefault(
                t => directiveName.Equals(t.Name.Value));

            if (directive is { Arguments: { Count: > 0 } })
            {
                ArgumentNode? argument = directive.Arguments.FirstOrDefault(
                    t => t.Name.Value.Equals("name"));

                if (argument is { Value: StringValueNode stringValue })
                {
                    return stringValue.Value;
                }
            }

            return null;
        }

        private static void CollectGlobalEntityPatterns(
            IEnumerable<SchemaExtensionNode> schemaExtensions,
            List<SelectionSetNode> entityPatterns)
        {
            foreach (var schemaExtension in schemaExtensions)
            {
                foreach (var directive in schemaExtension.Directives)
                {
                    if (TryGetKeys(directive, out SelectionSetNode? selectionSet))
                    {
                        entityPatterns.Add(selectionSet);
                    }
                }
            }
        }

        private static void CollectTypeEntityPatterns(
            IEnumerable<ObjectTypeExtensionNode> objectTypeExtensions,
            Dictionary<NameString, SelectionSetNode> entityPatterns)
        {
            foreach (ObjectTypeExtensionNode objectTypeExtension in objectTypeExtensions)
            {
                if (TryGetKeys(objectTypeExtension, out SelectionSetNode? selectionSet) &&
                    !entityPatterns.ContainsKey(objectTypeExtension.Name.Value))
                {
                    entityPatterns.Add(
                        objectTypeExtension.Name.Value,
                        selectionSet);
                }
            }
        }

        private static bool TryGetKeys(
            IHasDirectives directives,
            [NotNullWhen(true)] out SelectionSetNode? selectionSet)
        {
            DirectiveNode? directive = directives.Directives.FirstOrDefault(IsKeyDirective);

            if (directive is not null)
            {
                return TryGetKeys(directive, out selectionSet);
            }

            selectionSet = null;
            return false;
        }

        private static bool TryGetKeys(
            DirectiveNode directive,
            [NotNullWhen(true)] out SelectionSetNode? selectionSet)
        {
            if (directive is { Arguments: { Count: 1 } } &&
                directive.Arguments[0] is { Name: { Value: "fields" }, Value: StringValueNode sv})
            {
                selectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet($"{{{sv.Value}}}");
                return true;
            }

            selectionSet = null;
            return false;
        }

        private static bool IsKeyDirective(DirectiveNode directive) =>
            directive.Name.Value.Equals("key", StringComparison.Ordinal);
    }
}
