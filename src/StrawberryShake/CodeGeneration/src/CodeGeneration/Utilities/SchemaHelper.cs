using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers;

namespace StrawberryShake.CodeGeneration.Utilities
{
    public static class SchemaHelper
    {
        private static readonly HashSet<string> _knownScalars = new()
        {
            ScalarNames.String,
            ScalarNames.ID,
            ScalarNames.Boolean,
            ScalarNames.Byte,
            ScalarNames.Short,
            ScalarNames.Int,
            ScalarNames.Long,
            ScalarNames.Float,
            ScalarNames.Decimal,
            ScalarNames.Url,
            ScalarNames.Uuid,
            ScalarNames.DateTime,
            ScalarNames.Date,
            ScalarNames.ByteArray
        };

        public static ISchema Load(params (string, DocumentNode)[] documents)
        {
            return Load((IEnumerable<(string, DocumentNode)>)documents);
        }

        public static ISchema Load(IEnumerable<(string, DocumentNode)> documents)
        {
            if (documents is null)
            {
                throw new ArgumentNullException(nameof(documents));
            }

            ISchemaBuilder builder = SchemaBuilder.New();

            var leafTypes = new Dictionary<NameString, LeafTypeInfo>();
            var globalEntityPatterns = new List<SelectionSetNode>();
            var typeEntityPatterns = new Dictionary<NameString, SelectionSetNode>();

            foreach (DocumentNode document in documents.Select(doc => doc.Item2))
            {
                if (document.Definitions.Any(t => t is ITypeSystemExtensionNode))
                {
                    CollectScalarInfos(
                        document.Definitions.OfType<ScalarTypeExtensionNode>(),
                        leafTypes);

                    CollectEnumInfos(
                        document.Definitions.OfType<EnumTypeExtensionNode>(),
                        leafTypes);

                    CollectGlobalEntityPatterns(
                        document.Definitions.OfType<SchemaExtensionNode>(),
                        globalEntityPatterns);

                    CollectTypeEntityPatterns(
                        document.Definitions.OfType<ObjectTypeExtensionNode>(),
                        typeEntityPatterns);

                    AddDefaultScalarInfos(leafTypes);
                }
                else
                {
                    foreach (var scalar in document.Definitions.OfType<ScalarTypeDefinitionNode>())
                    {
                        if (!_knownScalars.Contains(scalar.Name.Value))
                        {
                            builder.AddType(new AnyType(
                                scalar.Name.Value,
                                scalar.Description.Value));
                        }
                    }

                    builder.AddDocument(document);
                }
            }

            return builder
                .TryAddTypeInterceptor(
                    new LeafTypeInterceptor(leafTypes))
                .TryAddTypeInterceptor(
                    new EntityTypeInterceptor(globalEntityPatterns, typeEntityPatterns))
                .Use(_ => _ => throw new NotSupportedException())
                .Create();
        }

        private static void CollectScalarInfos(
            IEnumerable<ScalarTypeExtensionNode> scalarTypeExtensions,
            Dictionary<NameString, LeafTypeInfo> leafTypes)
        {
            foreach (ScalarTypeExtensionNode scalarTypeExtension in scalarTypeExtensions)
            {
                if (!leafTypes.TryGetValue(
                    scalarTypeExtension.Name.Value,
                    out LeafTypeInfo scalarInfo))
                {
                    scalarInfo = new LeafTypeInfo(
                        scalarTypeExtension.Name.Value,
                        GetDirectiveValue(scalarTypeExtension, "runtimeType"),
                        GetDirectiveValue(scalarTypeExtension, "serializationType"));
                    leafTypes.Add(scalarInfo.TypeName, scalarInfo);
                }
            }
        }

        private static void CollectEnumInfos(
            IEnumerable<EnumTypeExtensionNode> enumTypeExtensions,
            Dictionary<NameString, LeafTypeInfo> leafTypes)
        {
            foreach (EnumTypeExtensionNode scalarTypeExtension in enumTypeExtensions)
            {
                if (!leafTypes.TryGetValue(
                    scalarTypeExtension.Name.Value,
                    out LeafTypeInfo scalarInfo))
                {
                    scalarInfo = new LeafTypeInfo(
                        scalarTypeExtension.Name.Value,
                        GetDirectiveValue(scalarTypeExtension, "runtimeType"),
                        GetDirectiveValue(scalarTypeExtension, "serializationType"));
                    leafTypes.Add(scalarInfo.TypeName, scalarInfo);
                }
            }
        }

        private static string? GetDirectiveValue(
            HotChocolate.Language.IHasDirectives hasDirectives,
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

        private static void AddDefaultScalarInfos(
            Dictionary<NameString, LeafTypeInfo> leafTypes)
        {
            TryAddLeafType(leafTypes, ScalarNames.String, TypeNames.String);
            TryAddLeafType(leafTypes, ScalarNames.ID, TypeNames.String);
            TryAddLeafType(leafTypes, ScalarNames.Boolean, TypeNames.Boolean, TypeNames.Boolean);
            TryAddLeafType(leafTypes, ScalarNames.Byte, TypeNames.Byte, TypeNames.Byte);
            TryAddLeafType(leafTypes, ScalarNames.Short, TypeNames.Int16, TypeNames.Int16);
            TryAddLeafType(leafTypes, ScalarNames.Int, TypeNames.Int32, TypeNames.Int32);
            TryAddLeafType(leafTypes, ScalarNames.Long, TypeNames.Int64, TypeNames.Int64);
            TryAddLeafType(leafTypes, ScalarNames.Float, TypeNames.Double, TypeNames.Double);
            TryAddLeafType(leafTypes, ScalarNames.Decimal, TypeNames.Decimal, TypeNames.Decimal);
            TryAddLeafType(leafTypes, ScalarNames.Url, TypeNames.Uri);
            TryAddLeafType(leafTypes, ScalarNames.Uuid, TypeNames.Guid, TypeNames.Guid);
            TryAddLeafType(leafTypes, "Guid", TypeNames.Guid, TypeNames.Guid);
            TryAddLeafType(leafTypes, ScalarNames.DateTime, TypeNames.DateTimeOffset);
            TryAddLeafType(leafTypes, ScalarNames.Date, TypeNames.DateTime);
            TryAddLeafType(leafTypes, ScalarNames.ByteArray, TypeNames.ByteArray);
        }

        private static bool TryGetKeys(
            HotChocolate.Language.IHasDirectives directives,
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
                directive.Arguments[0] is { Name: { Value: "fields" }, Value: StringValueNode sv })
            {
                selectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet($"{{{sv.Value}}}");
                return true;
            }

            selectionSet = null;
            return false;
        }

        private static bool IsKeyDirective(DirectiveNode directive) =>
            directive.Name.Value.Equals("key", StringComparison.Ordinal);

        private static void TryAddLeafType(
            Dictionary<NameString, LeafTypeInfo> leafTypes,
            NameString typeName,
            string runtimeType,
            string serializationType = TypeNames.String)
        {
            if (!leafTypes.TryGetValue(typeName, out LeafTypeInfo leafType))
            {
                leafType = new LeafTypeInfo(typeName, runtimeType, serializationType);
                leafTypes.Add(typeName, leafType);
            }
        }
    }
}
