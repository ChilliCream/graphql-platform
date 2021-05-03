using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers;
using StrawberryShake.CodeGeneration.Analyzers.Types;
using static StrawberryShake.CodeGeneration.Utilities.DocumentHelper;

namespace StrawberryShake.CodeGeneration.Utilities
{
    public static class SchemaHelper
    {
        private static string _typeInfosKey = "StrawberryShake.CodeGeneration.Utilities.TypeInfos";

        public static ISchema Load(
            IReadOnlyCollection<GraphQLFile> schemaFiles,
            bool strictValidation = true,
            bool noStore = false)
        {
            if (schemaFiles is null)
            {
                throw new ArgumentNullException(nameof(schemaFiles));
            }

            var typeInfos = new TypeInfos();
            var lookup = new Dictionary<ISyntaxNode, string>();
            IndexSyntaxNodes(schemaFiles, lookup);

            var builder = SchemaBuilder.New();

            builder.ModifyOptions(o => o.StrictValidation = strictValidation);

            var leafTypes = new Dictionary<NameString, LeafTypeInfo>();
            var globalEntityPatterns = new List<SelectionSetNode>();
            var typeEntityPatterns = new Dictionary<NameString, SelectionSetNode>();

            foreach (DocumentNode document in schemaFiles.Select(f => f.Document))
            {
                if (document.Definitions.Any(t => t is ITypeSystemExtensionNode))
                {
                    CollectScalarInfos(
                        document.Definitions.OfType<ScalarTypeExtensionNode>(),
                        leafTypes,
                        typeInfos);

                    CollectEnumInfos(
                        document.Definitions.OfType<EnumTypeExtensionNode>(),
                        leafTypes,
                        typeInfos);

                    if (!noStore)
                    {
                        CollectGlobalEntityPatterns(
                            document.Definitions.OfType<SchemaExtensionNode>(),
                            globalEntityPatterns);

                        CollectTypeEntityPatterns(
                            document.Definitions.OfType<ObjectTypeExtensionNode>(),
                            typeEntityPatterns);
                    }
                }
                else
                {
                    foreach (var scalar in document.Definitions.OfType<ScalarTypeDefinitionNode>())
                    {
                        if (!BuiltInScalarNames.IsBuiltInScalar(scalar.Name.Value))
                        {
                            builder.AddType(new AnyType(
                                scalar.Name.Value,
                                scalar.Description?.Value));
                        }
                    }

                    builder.AddDocument(document);
                }
            }

            AddDefaultScalarInfos(leafTypes);

            return builder
                .SetSchema(d => d.Extend().OnBeforeCreate(
                    c => c.ContextData.Add(_typeInfosKey, typeInfos)))
                .TryAddTypeInterceptor(
                    new LeafTypeInterceptor(leafTypes))
                .TryAddTypeInterceptor(
                    new EntityTypeInterceptor(globalEntityPatterns, typeEntityPatterns))
                .Use(_ => _ => throw new NotSupportedException())
                .Create();
        }

        public static RuntimeTypeInfo GetOrCreateTypeInfo(
            this ISchema schema,
            string typeName,
            bool valueType = false) =>
            ((TypeInfos)schema.ContextData[_typeInfosKey]!).GetOrCreate(typeName, valueType);

        private static void CollectScalarInfos(
            IEnumerable<ScalarTypeExtensionNode> scalarTypeExtensions,
            Dictionary<NameString, LeafTypeInfo> leafTypes,
            TypeInfos typeInfos)
        {
            foreach (ScalarTypeExtensionNode scalarTypeExtension in scalarTypeExtensions)
            {
                if (!leafTypes.TryGetValue(
                    scalarTypeExtension.Name.Value,
                    out LeafTypeInfo scalarInfo))
                {
                    var runtimeType = GetRuntimeType(scalarTypeExtension);
                    var serializationType = GetSerializationType(scalarTypeExtension);

                    TryRegister(typeInfos, runtimeType);
                    TryRegister(typeInfos, serializationType);

                    scalarInfo = new LeafTypeInfo(
                        scalarTypeExtension.Name.Value,
                        runtimeType?.Name,
                        serializationType?.Name);

                    leafTypes.Add(scalarInfo.TypeName, scalarInfo);
                }
            }
        }

        private static void CollectEnumInfos(
            IEnumerable<EnumTypeExtensionNode> enumTypeExtensions,
            Dictionary<NameString, LeafTypeInfo> leafTypes,
            TypeInfos typeInfos)
        {
            foreach (EnumTypeExtensionNode scalarTypeExtension in enumTypeExtensions)
            {
                if (!leafTypes.TryGetValue(
                    scalarTypeExtension.Name.Value,
                    out LeafTypeInfo scalarInfo))
                {
                    var runtimeType = GetRuntimeType(scalarTypeExtension);
                    var serializationType = GetSerializationType(scalarTypeExtension);

                    TryRegister(typeInfos, runtimeType);
                    TryRegister(typeInfos, serializationType);

                    scalarInfo = new LeafTypeInfo(
                        scalarTypeExtension.Name.Value,
                        runtimeType?.Name,
                        serializationType?.Name);
                    leafTypes.Add(scalarInfo.TypeName, scalarInfo);
                }
            }
        }

        private static RuntimeTypeDirective? GetRuntimeType(
            HotChocolate.Language.IHasDirectives hasDirectives) =>
            GetDirectiveValue(hasDirectives, "runtimeType");

        private static RuntimeTypeDirective? GetSerializationType(
            HotChocolate.Language.IHasDirectives hasDirectives) =>
            GetDirectiveValue(hasDirectives, "serializationType");

        private static RuntimeTypeDirective? GetDirectiveValue(
            HotChocolate.Language.IHasDirectives hasDirectives,
            NameString directiveName)
        {
            DirectiveNode? directive = hasDirectives.Directives.FirstOrDefault(
                t => directiveName.Equals(t.Name.Value));

            if (directive is { Arguments: { Count: > 0 } })
            {
                ArgumentNode? name = directive.Arguments.FirstOrDefault(
                    t => t.Name.Value.Equals("name"));
                ArgumentNode? valueType = directive.Arguments.FirstOrDefault(
                    t => t.Name.Value.Equals("valueType"));

                if (name is { Value: StringValueNode stringValue })
                {
                    BooleanValueNode? valueTypeValue = valueType?.Value as BooleanValueNode;
                    return new(stringValue.Value, valueTypeValue?.Value);
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
            TryAddLeafType(leafTypes, ScalarNames.TimeSpan, TypeNames.TimeSpan);
            TryAddLeafType(leafTypes, ScalarNames.ByteArray, TypeNames.ByteArray, TypeNames.ByteArray);
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

        private static void TryRegister(TypeInfos typeInfos, RuntimeTypeDirective? runtimeType)
        {
            if (runtimeType is not null)
            {
                typeInfos.TryCreate(runtimeType);
            }
        }
    }
}
