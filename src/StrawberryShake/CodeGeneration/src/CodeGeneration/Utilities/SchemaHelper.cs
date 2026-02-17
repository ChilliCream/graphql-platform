using System.Diagnostics.CodeAnalysis;
using HotChocolate;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using StrawberryShake.CodeGeneration.Analyzers;
using StrawberryShake.CodeGeneration.Analyzers.Types;
using static StrawberryShake.CodeGeneration.Utilities.DocumentHelper;

namespace StrawberryShake.CodeGeneration.Utilities;

public static class SchemaHelper
{
    public static Schema Load(
        IReadOnlyCollection<GraphQLFile> schemaFiles,
        bool strictValidation = true,
        bool noStore = false)
    {
        ArgumentNullException.ThrowIfNull(schemaFiles);

        var typeInfos = new TypeInfos();
        var lookup = new Dictionary<ISyntaxNode, string>();
        IndexSyntaxNodes(schemaFiles, lookup);

        var builder = SchemaBuilder.New();
        builder.Features.Set(typeInfos);

        builder.ModifyOptions(o => o.StrictValidation = strictValidation);

        var leafTypes = new Dictionary<string, LeafTypeInfo>(StringComparer.Ordinal);
        var globalEntityPatterns = new List<SelectionSetNode>();
        var typeEntityPatterns = new Dictionary<string, SelectionSetNode>(StringComparer.Ordinal);

        foreach (var document in schemaFiles.Select(f => f.Document))
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
                    else if (scalar.Name.Value == ScalarNames.Any)
                    {
                        builder.AddType(new AnyType());
                    }
                    else if (scalar.Name.Value == "JSON")
                    {
                        builder.AddType(new AnyType());
                    }
                }

                builder.AddDocument(document);
            }
        }

        AddDefaultScalarInfos(builder, leafTypes);

        return builder
            .ModifyOptions(
                o =>
                {
                    o.EnableDefer = true;
                    o.EnableStream = true;
                    o.EnableTag = false;
                    o.EnableOneOf = true;
                    o.EnableFlagEnums = false;
                })
            .SetSchema(d => d.Extend().OnBeforeCreate(
                c => c.Features.Set(typeInfos)))
            .TryAddTypeInterceptor(
                new LeafTypeInterceptor(leafTypes))
            .TryAddTypeInterceptor(
                new EntityTypeInterceptor(globalEntityPatterns, typeEntityPatterns))
            .Use(_ => _ => throw new NotSupportedException())
            .Create();
    }

    public static RuntimeTypeInfo GetOrCreateTypeInfo(
        this Schema schema,
        string typeName,
        bool valueType = false)
        => schema.Features.GetOrSet<TypeInfos>().GetOrAdd(typeName, valueType);

    private static void CollectScalarInfos(
        IEnumerable<ScalarTypeExtensionNode> scalarTypeExtensions,
        Dictionary<string, LeafTypeInfo> leafTypes,
        TypeInfos typeInfos)
    {
        foreach (var scalarTypeExtension in scalarTypeExtensions)
        {
            if (!leafTypes.ContainsKey(scalarTypeExtension.Name.Value))
            {
                var runtimeType = GetRuntimeType(scalarTypeExtension);
                var serializationType = GetSerializationType(scalarTypeExtension);

                TryRegister(typeInfos, runtimeType);
                TryRegister(typeInfos, serializationType);

                var scalarInfo = new LeafTypeInfo(
                    scalarTypeExtension.Name.Value,
                    runtimeType?.Name,
                    serializationType?.Name);

                leafTypes.Add(scalarInfo.TypeName, scalarInfo);
            }
        }
    }

    private static void CollectEnumInfos(
        IEnumerable<EnumTypeExtensionNode> enumTypeExtensions,
        Dictionary<string, LeafTypeInfo> leafTypes,
        TypeInfos typeInfos)
    {
        foreach (var scalarTypeExtension in enumTypeExtensions)
        {
            if (!leafTypes.ContainsKey(scalarTypeExtension.Name.Value))
            {
                var runtimeType = GetRuntimeType(scalarTypeExtension);
                var serializationType = GetSerializationType(scalarTypeExtension);

                TryRegister(typeInfos, runtimeType);
                TryRegister(typeInfos, serializationType);

                var scalarInfo = new LeafTypeInfo(
                    scalarTypeExtension.Name.Value,
                    runtimeType?.Name,
                    serializationType?.Name);
                leafTypes.Add(scalarInfo.TypeName, scalarInfo);
            }
        }
    }

    private static RuntimeTypeDirective? GetRuntimeType(IHasDirectives hasDirectives)
        => GetDirectiveValue(hasDirectives, "runtimeType");

    private static RuntimeTypeDirective? GetSerializationType(IHasDirectives hasDirectives)
        => GetDirectiveValue(hasDirectives, "serializationType");

    private static RuntimeTypeDirective? GetDirectiveValue(
        IHasDirectives hasDirectives,
        string directiveName)
    {
        var directive = hasDirectives.Directives.FirstOrDefault(
            t => directiveName.EqualsOrdinal(t.Name.Value));

        if (directive is { Arguments.Count: > 0 })
        {
            var name = directive.Arguments.FirstOrDefault(
                t => t.Name.Value.Equals("name"));
            var valueType = directive.Arguments.FirstOrDefault(
                t => t.Name.Value.Equals("valueType"));

            if (name is { Value: StringValueNode stringValue })
            {
                var valueTypeValue = valueType?.Value as BooleanValueNode;
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
                if (TryGetKeys(directive, out var selectionSet))
                {
                    entityPatterns.Add(selectionSet);
                }
            }
        }
    }

    private static void CollectTypeEntityPatterns(
        IEnumerable<ObjectTypeExtensionNode> objectTypeExtensions,
        Dictionary<string, SelectionSetNode> entityPatterns)
    {
        foreach (var objectTypeExtension in objectTypeExtensions)
        {
            if (TryGetKeys(objectTypeExtension, out var selectionSet))
            {
                entityPatterns.TryAdd(objectTypeExtension.Name.Value, selectionSet);
            }
        }
    }

    private static void AddDefaultScalarInfos(
        ISchemaBuilder schemaBuilder,
        Dictionary<string, LeafTypeInfo> leafTypes)
    {
        TryAddLeafType(
            leafTypes,
            typeName: ScalarNames.Any,
            runtimeType: TypeNames.JsonElement,
            serializationType: TypeNames.JsonElement);
        TryAddLeafType(
            leafTypes,
            typeName: ScalarNames.Base64String,
            runtimeType: TypeNames.ByteArray,
            serializationType: TypeNames.ByteArray);
        TryAddLeafType(
            leafTypes,
            typeName: ScalarNames.Boolean,
            runtimeType: TypeNames.Boolean,
            serializationType: TypeNames.Boolean);
        TryAddLeafType(
            leafTypes,
            typeName: ScalarNames.Byte,
            runtimeType: TypeNames.SByte,
            serializationType: TypeNames.SByte);
        TryAddLeafType(
            leafTypes,
            typeName: ScalarNames.ByteArray,
            runtimeType: TypeNames.ByteArray,
            serializationType: TypeNames.ByteArray);
        TryAddLeafType(
            leafTypes,
            typeName: ScalarNames.Date,
            runtimeType: TypeNames.DateOnly);
        TryAddLeafType(
            leafTypes,
            typeName: ScalarNames.DateTime,
            runtimeType: TypeNames.DateTimeOffset);
        TryAddLeafType(
            leafTypes,
            typeName: ScalarNames.Decimal,
            runtimeType: TypeNames.Decimal,
            serializationType: TypeNames.Decimal);
        TryAddLeafType(
            leafTypes,
            typeName: ScalarNames.Float,
            runtimeType: TypeNames.Double,
            serializationType: TypeNames.Double);
        TryAddLeafType(
            leafTypes,
            typeName: "Guid",
            runtimeType: TypeNames.Guid,
            serializationType: TypeNames.String);
        TryAddLeafType(
            leafTypes,
            typeName: ScalarNames.ID,
            runtimeType: TypeNames.String);
        TryAddLeafType(
            leafTypes,
            typeName: ScalarNames.Int,
            runtimeType: TypeNames.Int32,
            serializationType: TypeNames.Int32);
        TryAddLeafType(
            leafTypes,
            typeName: "Json",
            runtimeType: TypeNames.JsonElement,
            serializationType: TypeNames.JsonElement);
        TryAddLeafType(
            leafTypes,
            typeName: "JSON",
            runtimeType: TypeNames.JsonElement,
            serializationType: TypeNames.JsonElement);
        TryAddLeafType(
            leafTypes,
            typeName: ScalarNames.LocalDate,
            runtimeType: TypeNames.DateOnly);
        TryAddLeafType(
            leafTypes,
            typeName: ScalarNames.LocalDateTime,
            runtimeType: TypeNames.DateTime);
        TryAddLeafType(
            leafTypes,
            typeName: ScalarNames.LocalTime,
            runtimeType: TypeNames.TimeOnly);
        TryAddLeafType(
            leafTypes,
            typeName: ScalarNames.Long,
            runtimeType: TypeNames.Int64,
            serializationType: TypeNames.Int64);
        TryAddLeafType(
            leafTypes,
            typeName: ScalarNames.Short,
            runtimeType: TypeNames.Int16,
            serializationType: TypeNames.Int16);
        TryAddLeafType(
            leafTypes,
            typeName: ScalarNames.String,
            runtimeType: TypeNames.String);
        TryAddLeafType(
            leafTypes,
            typeName: ScalarNames.TimeSpan,
            runtimeType: TypeNames.TimeSpan);
        TryAddLeafType(
            leafTypes,
            typeName: ScalarNames.UnsignedByte,
            runtimeType: TypeNames.Byte,
            serializationType: TypeNames.Byte);
        TryAddLeafType(
            leafTypes,
            typeName: ScalarNames.UnsignedInt,
            runtimeType: TypeNames.UInt32,
            serializationType: TypeNames.UInt32);
        TryAddLeafType(
            leafTypes,
            typeName: ScalarNames.UnsignedLong,
            runtimeType: TypeNames.UInt64,
            serializationType: TypeNames.UInt64);
        TryAddLeafType(
            leafTypes,
            typeName: ScalarNames.UnsignedShort,
            runtimeType: TypeNames.UInt16,
            serializationType: TypeNames.UInt16);
        TryAddLeafType(
            leafTypes,
            typeName: "Upload",
            runtimeType: TypeNames.Upload,
            serializationType: TypeNames.String);
        TryAddLeafType(
            leafTypes,
            typeName: ScalarNames.URI,
            runtimeType: TypeNames.Uri);
        TryAddLeafType(
            leafTypes,
            typeName: "Uri",
            runtimeType: TypeNames.Uri);
        TryAddLeafType(
            leafTypes,
            typeName: ScalarNames.URL,
            runtimeType: TypeNames.Uri);
        TryAddLeafType(
            leafTypes,
            typeName: "Url",
            runtimeType: TypeNames.Uri);
        TryAddLeafType(
            leafTypes,
            typeName: ScalarNames.UUID,
            runtimeType: TypeNames.Guid,
            serializationType: TypeNames.String);
        TryAddLeafType(
            leafTypes,
            typeName: "Uuid",
            runtimeType: TypeNames.Guid,
            serializationType: TypeNames.String);

        // register aliases
        schemaBuilder.AddType(new UriType());
        schemaBuilder.AddType(new UriType("Uri"));
        schemaBuilder.AddType(new UrlType());
        schemaBuilder.AddType(new UrlType("Url"));
        schemaBuilder.AddType(new UuidType());
        schemaBuilder.AddType(new UuidType("Guid"));
        schemaBuilder.AddType(new UuidType("Uuid"));
    }

    private static bool TryGetKeys(
        IHasDirectives directives,
        [NotNullWhen(true)] out SelectionSetNode? selectionSet)
    {
        var directive = directives.Directives.FirstOrDefault(IsKeyDirective);

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
        if (directive is { Arguments: { Count: 1 } }
            && directive.Arguments[0] is { Name: { Value: "fields" }, Value: StringValueNode sv })
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
        Dictionary<string, LeafTypeInfo> leafTypes,
        string typeName,
        string runtimeType,
        string serializationType = TypeNames.String)
    {
        if (!leafTypes.ContainsKey(typeName))
        {
            var leafType = new LeafTypeInfo(typeName, runtimeType, serializationType);
            leafTypes.Add(typeName, leafType);
        }
    }

    private static void TryRegister(TypeInfos typeInfos, RuntimeTypeDirective? runtimeType)
    {
        if (runtimeType is not null)
        {
            typeInfos.GetOrAdd(runtimeType);
        }
    }
}
