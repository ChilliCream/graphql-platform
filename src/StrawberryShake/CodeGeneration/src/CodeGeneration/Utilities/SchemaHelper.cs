using System.Diagnostics.CodeAnalysis;
using HotChocolate;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;
using HotChocolate.Utilities;
using StrawberryShake.CodeGeneration.Analyzers;
using StrawberryShake.CodeGeneration.Analyzers.Types;
using static StrawberryShake.CodeGeneration.Utilities.DocumentHelper;

namespace StrawberryShake.CodeGeneration.Utilities;

public static class SchemaHelper
{
    public static MutableSchemaDefinition Load(
        IReadOnlyCollection<GraphQLFile> schemaFiles,
        bool strictValidation = true,
        bool noStore = false)
    {
        ArgumentNullException.ThrowIfNull(schemaFiles);
        _ = strictValidation;

        var typeInfos = new TypeInfos();
        var lookup = new Dictionary<ISyntaxNode, string>();
        IndexSyntaxNodes(schemaFiles, lookup);

        var schema = new MutableSchemaDefinition();
        schema.Features.Set(typeInfos);

        var leafTypes = new Dictionary<string, LeafTypeInfo>(StringComparer.Ordinal);
        var globalEntityPatterns = new List<SelectionSetNode>();
        var typeEntityPatterns = new Dictionary<string, SelectionSetNode>(StringComparer.Ordinal);
        var definitions = new List<IDefinitionNode>();
        var parserOptions = new SchemaParserOptions
        {
            IgnoreExistingTypes = true,
            IgnoreExistingDirectives = true
        };

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
                definitions.AddRange(document.Definitions.Select(NormalizeDefinition));
            }
        }

        AddDefaultScalarInfos(leafTypes);
        AddImplicitScalarDefinitions(schema, leafTypes.Keys, definitions);
        AddBuiltInDirectiveDefinitions(schema);
        SchemaParser.Parse(schema, IntrospectionSchema, parserOptions);

        if (definitions.Count > 0)
        {
            SchemaParser.Parse(schema, new DocumentNode(definitions), parserOptions);
        }

        AddIntrospectionFields(schema);
        AnnotateSchema(schema, leafTypes, globalEntityPatterns, typeEntityPatterns);

        return schema;
    }

    public static RuntimeTypeInfo GetOrCreateTypeInfo(
        this MutableSchemaDefinition schema,
        string typeName,
        bool valueType = false)
        => schema.Features.GetOrSet<TypeInfos>().GetOrAdd(typeName, valueType);

    private static IDefinitionNode NormalizeDefinition(IDefinitionNode definition)
        => definition is SchemaDefinitionNode schemaDefinition
            ? schemaDefinition.WithOperationTypes(
                schemaDefinition.OperationTypes
                    .Where(t => !t.Type.Name.Value.Equals("null", StringComparison.Ordinal))
                    .ToArray())
            : definition;

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
            t => directiveName.Equals(t.Name.Value, StringComparison.Ordinal));

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

    private static void AddDefaultScalarInfos(Dictionary<string, LeafTypeInfo> leafTypes)
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
            typeName: ScalarNames.Duration,
            runtimeType: TypeNames.TimeSpan);
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
            typeName: "TimeSpan",
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
    }

    private static void AddImplicitScalarDefinitions(
        MutableSchemaDefinition schema,
        IEnumerable<string> scalarNames,
        IEnumerable<IDefinitionNode> definitions)
    {
        var declaredTypeNames =
            definitions
                .OfType<ITypeDefinitionNode>()
                .Select(t => t.Name.Value)
                .ToHashSet(StringComparer.Ordinal);

        foreach (var scalarName in scalarNames)
        {
            if (!declaredTypeNames.Contains(scalarName))
            {
                TryAddScalarDefinition(schema, scalarName);
            }
        }
    }

    private static void AddBuiltInDirectiveDefinitions(MutableSchemaDefinition schema)
    {
        TryAddDirectiveDefinition(schema, BuiltIns.Include.Create(schema));
        TryAddDirectiveDefinition(schema, BuiltIns.Skip.Create(schema));
        TryAddDirectiveDefinition(schema, BuiltIns.Deprecated.Create(schema));
        TryAddDirectiveDefinition(schema, BuiltIns.SpecifiedBy.Create(schema));
        TryAddDirectiveDefinition(schema, BuiltIns.OneOf.Create());
    }

    private static void AddIntrospectionFields(MutableSchemaDefinition schema)
    {
        if (schema.QueryType is null)
        {
            return;
        }

        var queryType = schema.QueryType;
        var schemaType = schema.Types.GetType<MutableObjectTypeDefinition>("__Schema");
        var typeType = schema.Types.GetType<MutableObjectTypeDefinition>("__Type");
        var stringType = schema.Types.GetType<MutableScalarTypeDefinition>(ScalarNames.String);

        if (!queryType.Fields.ContainsName("__schema"))
        {
            queryType.Fields.Add(
                new MutableOutputFieldDefinition("__schema", new NonNullType(schemaType))
                {
                    Description = "Access the current type schema of this server.",
                    DeclaringMember = queryType,
                    IsIntrospectionField = true,
                    Flags = FieldFlags.Introspection | FieldFlags.SchemaIntrospectionField
                });
        }

        if (!queryType.Fields.ContainsName("__type"))
        {
            var field = new MutableOutputFieldDefinition("__type", typeType)
            {
                Description = "Request the type information of a single type.",
                DeclaringMember = queryType,
                IsIntrospectionField = true,
                Flags = FieldFlags.Introspection | FieldFlags.TypeIntrospectionField
            };

            field.Arguments.Add(
                new MutableInputFieldDefinition("name", new NonNullType(stringType))
                {
                    DeclaringMember = field
                });

            queryType.Fields.Add(field);
        }
    }

    private static void TryAddDirectiveDefinition(
        MutableSchemaDefinition schema,
        MutableDirectiveDefinition directiveDefinition)
    {
        if (!schema.DirectiveDefinitions.ContainsName(directiveDefinition.Name))
        {
            schema.DirectiveDefinitions.Add(directiveDefinition);
        }
    }

    private static void TryAddScalarDefinition(MutableSchemaDefinition schema, string typeName)
    {
        if (!schema.Types.TryGetType(typeName, out _))
        {
            schema.Types.Add(new MutableScalarTypeDefinition(typeName) { IsSpecScalar = true });
        }
    }

    private static void AnnotateSchema(
        MutableSchemaDefinition schema,
        Dictionary<string, LeafTypeInfo> leafTypes,
        IReadOnlyList<SelectionSetNode> globalEntityPatterns,
        IReadOnlyDictionary<string, SelectionSetNode> typeEntityPatterns)
    {
        foreach (var type in schema.Types)
        {
            if (!type.IsLeafType())
            {
                continue;
            }

            if (leafTypes.TryGetValue(type.Name, out var leafType))
            {
                type.Features.Set(type is IScalarTypeDefinition
                    ? new LeafTypeFeature(leafType.RuntimeType, leafType.SerializationType)
                    : new LeafTypeFeature(null, leafType.SerializationType));
            }
            else
            {
                type.Features.Set(type is IScalarTypeDefinition
                    ? new LeafTypeFeature(TypeNames.String, TypeNames.String)
                    : new LeafTypeFeature(null, TypeNames.String));
            }
        }

        var complexTypes = new List<IComplexTypeDefinition>();

        foreach (var type in schema.Types)
        {
            if (type is not IComplexTypeDefinition complexType)
            {
                continue;
            }

            if (typeEntityPatterns.TryGetValue(complexType.Name, out var pattern))
            {
                complexType.Features.Set(new EntityFeature(pattern));
            }
            else
            {
                complexTypes.Add(complexType);
            }
        }

        if (globalEntityPatterns.Count == 0)
        {
            return;
        }

        foreach (var complexType in complexTypes)
        {
            if (globalEntityPatterns.FirstOrDefault(
                pattern => DoesPatternMatch(complexType, pattern)) is { } matchedPattern)
            {
                complexType.Features.Set(new EntityFeature(matchedPattern));
            }
        }
    }

    private static bool DoesPatternMatch(IComplexTypeDefinition outputType, SelectionSetNode pattern)
    {
        foreach (var selection in pattern.Selections.OfType<FieldNode>())
        {
            if (selection.SelectionSet is null
                && outputType.Fields.TryGetField(selection.Name.Value, out var field)
                && field.Type.NamedType().IsLeafType())
            {
                continue;
            }

            return false;
        }

        return true;
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

    private const string IntrospectionSchema =
        """
        "A GraphQL Schema defines the capabilities of a GraphQL server. It exposes all available types and directives on the server, as well as the entry points for query, mutation, and subscription operations."
        type __Schema {
          description: String
          "A list of all types supported by this server."
          types: [__Type!]!
          "The type that query operations will be rooted at."
          queryType: __Type!
          "If this server supports mutation, the type that mutation operations will be rooted at."
          mutationType: __Type
          "If this server support subscription, the type that subscription operations will be rooted at."
          subscriptionType: __Type
          "A list of all directives supported by this server."
          directives: [__Directive!]!
        }

        "The fundamental unit of any GraphQL Schema is the type. There are many kinds of types in GraphQL as represented by the `__TypeKind` enum.\n\nDepending on the kind of a type, certain fields describe information about that type. Scalar types provide no information beyond a name and description, while Enum types provide their values. Object and Interface types provide the fields they describe. Abstract types, Union and Interface, provide the Object types possible at runtime. List and NonNull types compose other types."
        type __Type {
          kind: __TypeKind!
          name: String
          description: String
          specifiedByURL: String
          fields(includeDeprecated: Boolean! = false): [__Field!]
          interfaces: [__Type!]
          possibleTypes: [__Type!]
          enumValues(includeDeprecated: Boolean! = false): [__EnumValue!]
          inputFields(includeDeprecated: Boolean! = false): [__InputValue!]
          ofType: __Type
          isOneOf: Boolean
        }

        "An enum describing what kind of type a given `__Type` is."
        enum __TypeKind {
          "Indicates this type is a scalar."
          SCALAR
          "Indicates this type is an object. `fields` and `interfaces` are valid fields."
          OBJECT
          "Indicates this type is an interface. `fields` and `possibleTypes` are valid fields."
          INTERFACE
          "Indicates this type is a union. `possibleTypes` is a valid field."
          UNION
          "Indicates this type is an enum. `enumValues` is a valid field."
          ENUM
          "Indicates this type is an input object. `inputFields` is a valid field."
          INPUT_OBJECT
          "Indicates this type is a list. `ofType` is a valid field."
          LIST
          "Indicates this type is a non-null. `ofType` is a valid field."
          NON_NULL
        }

        "Object and Interface types are described by a list of Fields, each of which has a name, potentially a list of arguments, and a return type."
        type __Field {
          name: String!
          description: String
          args(includeDeprecated: Boolean! = false): [__InputValue!]!
          type: __Type!
          isDeprecated: Boolean!
          deprecationReason: String
        }

        "Arguments provided to Fields or Directives and the input fields of an InputObject are represented as Input Values which describe their type and optionally a default value."
        type __InputValue {
          name: String!
          description: String
          type: __Type!
          "A GraphQL-formatted string representing the default value for this input value."
          defaultValue: String
          isDeprecated: Boolean!
          deprecationReason: String
        }

        "One possible value for a given Enum. Enum values are unique values, not a placeholder for a string or numeric value. However an Enum value is returned in a JSON response as a string."
        type __EnumValue {
          name: String!
          description: String
          isDeprecated: Boolean!
          deprecationReason: String
        }

        "A Directive provides a way to describe alternate runtime execution and type validation behavior in a GraphQL document.\n\nIn some cases, you need to provide options to alter GraphQL's execution behavior in ways field arguments will not suffice, such as conditionally including or skipping a field. Directives provide this by describing additional information to the executor."
        type __Directive {
          name: String!
          description: String
          isRepeatable: Boolean!
          locations: [__DirectiveLocation!]!
          args(includeDeprecated: Boolean! = false): [__InputValue!]!
          onOperation: Boolean!
          onFragment: Boolean!
          onField: Boolean!
        }

        enum __DirectiveLocation {
          QUERY
          MUTATION
          SUBSCRIPTION
          FIELD
          FRAGMENT_DEFINITION
          FRAGMENT_SPREAD
          INLINE_FRAGMENT
          VARIABLE_DEFINITION
          SCHEMA
          SCALAR
          OBJECT
          FIELD_DEFINITION
          ARGUMENT_DEFINITION
          INTERFACE
          UNION
          ENUM
          ENUM_VALUE
          INPUT_OBJECT
          INPUT_FIELD_DEFINITION
        }
        """;
}
