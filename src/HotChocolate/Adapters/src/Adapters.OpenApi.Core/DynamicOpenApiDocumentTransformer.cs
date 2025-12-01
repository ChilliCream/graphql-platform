using System.Collections.Concurrent;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.AspNetCore.OpenApi;
#if NET10_0_OR_GREATER
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using OpenApiSchemaAbstraction = Microsoft.OpenApi.IOpenApiSchema;
#else
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using OpenApiSchemaAbstraction = Microsoft.OpenApi.Models.OpenApiSchema;
using OperationType = Microsoft.OpenApi.Models.OperationType;
#endif

namespace HotChocolate.Adapters.OpenApi;

internal sealed class DynamicOpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
    private List<OperationDescriptor> _operations = [];
    private List<ComponentDescriptor> _components = [];

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        foreach (var operation in _operations)
        {
            AddOperation(document, operation.Path, operation.HttpMethod, operation.Operation);
        }

        foreach (var component in _components)
        {
            AddComponent(document, component.SchemaName, component.Schema);
        }

        return Task.CompletedTask;
    }

    public void AddDocuments(
        OpenApiOperationDocument[] operations,
        OpenApiFragmentDocument[] fragments,
        ISchemaDefinition schema)
    {
        var operationDescriptors = new List<OperationDescriptor>();
        var componentDescriptors = new List<ComponentDescriptor>();

        var fragmentDocumentLookup = fragments.ToDictionary(f => f.Name);

        foreach (var operation in operations)
        {
            var operationDescriptor = CreateOperationDescriptor(operation, schema, fragmentDocumentLookup);
            operationDescriptors.Add(operationDescriptor);
        }

        foreach (var fragment in fragments)
        {
            var componentDescriptor = CreateComponentDescriptor(fragment, schema, fragmentDocumentLookup);
            componentDescriptors.Add(componentDescriptor);
        }

        _operations = operationDescriptors;
        _components = componentDescriptors;
    }

    private const string JsonContentType = "application/json";

    private static OperationDescriptor CreateOperationDescriptor(
        OpenApiOperationDocument operationDocument,
        ISchemaDefinition schema,
        Dictionary<string, OpenApiFragmentDocument> fragmentDocumentLookup)
    {
        var operation = new OpenApiOperation
        {
            Description = operationDocument.Description,
            OperationId = OpenApiHelpers.GetOperationId(operationDocument.Name),
            Parameters = [],
            Responses = []
        };

        var bodyParameter = operationDocument.BodyParameter;
        var bodyVariable = bodyParameter?.VariableName;
        var bodyVariableTrie = new InputObjectPathTrie();

        foreach (var routeParameter in operationDocument.Route.Parameters)
        {
            var parameter = CreateOpenApiParameter(operationDocument, routeParameter, ParameterLocation.Path, schema);

            operation.Parameters.Add(parameter);

            if (routeParameter.VariableName == bodyVariable && routeParameter.InputObjectPath is not null)
            {
                bodyVariableTrie.Add(routeParameter);
            }
        }

        foreach (var queryParameter in operationDocument.QueryParameters)
        {
            var parameter = CreateOpenApiParameter(operationDocument, queryParameter, ParameterLocation.Query, schema);

            operation.Parameters.Add(parameter);

            if (queryParameter.VariableName == bodyVariable && queryParameter.InputObjectPath is not null)
            {
                bodyVariableTrie.Add(queryParameter);
            }
        }

        if (bodyParameter is not null)
        {
            var graphqlType = GetGraphQLType(operationDocument.OperationDefinition, bodyParameter, schema);
            var requestBodyType = CreateOpenApiSchemaForType(graphqlType, schema);

            RemovePropertiesFromSchema(requestBodyType, bodyVariableTrie);

            var requestBody = new OpenApiRequestBody
            {
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    [JsonContentType] = new() { Schema = requestBodyType }
                }
            };

            operation.RequestBody = requestBody;
        }

        var operationType = schema.GetOperationType(operationDocument.OperationDefinition.Operation);

        if (operationDocument.OperationDefinition.SelectionSet.Selections is not [FieldNode rootField])
        {
            throw new InvalidOperationException("Expected to have a single field selection on the root");
        }

        var fieldType = operationType.Fields[rootField.Name.Value].Type;

        var responseSchema = rootField.SelectionSet is not null
            ? CreateOpenApiSchemaForSelectionSet(
                rootField.SelectionSet,
                fieldType,
                schema,
                operationDocument.LocalFragmentLookup,
                fragmentDocumentLookup)
            : CreateOpenApiSchemaForType(fieldType, schema);

        var responseBody = new OpenApiMediaType
        {
            Schema = responseSchema
        };

        operation.Responses["200"] = new OpenApiResponse
        {
            Content = new ConcurrentDictionary<string, OpenApiMediaType> { [JsonContentType] = responseBody }
        };

        return new OperationDescriptor(
            operationDocument.Route.ToOpenApiPath(),
            operationDocument.HttpMethod,
            operation);
    }

    private static ComponentDescriptor CreateComponentDescriptor(
        OpenApiFragmentDocument fragmentDocument,
        ISchemaDefinition schema,
        Dictionary<string, OpenApiFragmentDocument> fragmentDocumentLookup)
    {
        var componentSchema = CreateOpenApiSchemaForSelectionSet(
            fragmentDocument.FragmentDefinition.SelectionSet,
            (IOutputType)fragmentDocument.TypeCondition,
            schema,
            fragmentDocument.LocalFragmentLookup,
            fragmentDocumentLookup);

        componentSchema.Description = fragmentDocument.Description;

        return new ComponentDescriptor(fragmentDocument.Name, componentSchema);
    }

    private static void AddComponent(
        OpenApiDocument document,
        string schemaName,
        OpenApiSchemaAbstraction schema)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, OpenApiSchemaAbstraction>();

        document.Components.Schemas.Add(schemaName, schema);
    }

    private static void AddOperation(
        OpenApiDocument document,
        string path,
        string httpMethod,
        OpenApiOperation operation)
    {
#if NET10_0_OR_GREATER
        if (!document.Paths.TryGetValue(path, out var pathItem))
        {
            var concretePathItem = new OpenApiPathItem { Operations = [] };

            pathItem = concretePathItem;

            document.Paths.Add(path, pathItem);
        }

        if (pathItem.Operations is null)
        {
            throw new InvalidOperationException($"Expected {nameof(pathItem.Operations)} to be initialized.");
        }

        var method = GetHttpMethod(httpMethod);

        pathItem.Operations[method] = operation;
#else
        if (!document.Paths.TryGetValue(path, out var pathItem))
        {
            pathItem = new OpenApiPathItem();
            document.Paths.Add(path, pathItem);
        }

        var operationType = GetOperationType(httpMethod);

        document.Paths[path].Operations[operationType] = operation;
#endif
    }

#if NET10_0_OR_GREATER
    private static HttpMethod GetHttpMethod(string httpMethod)
    {
        return httpMethod.ToUpperInvariant() switch
        {
            "GET" => HttpMethod.Get,
            "POST" => HttpMethod.Post,
            "PUT" => HttpMethod.Put,
            "DELETE" => HttpMethod.Delete,
            "PATCH" => HttpMethod.Patch,
            _ => throw new NotSupportedException($"HTTP method {httpMethod} is not supported.")
        };
    }
#else
    private static OperationType GetOperationType(string httpMethod)
    {
        return httpMethod.ToUpperInvariant() switch
        {
            "GET" => OperationType.Get,
            "POST" => OperationType.Post,
            "PUT" => OperationType.Put,
            "DELETE" => OperationType.Delete,
            "PATCH" => OperationType.Patch,
            _ => throw new NotSupportedException($"HTTP method {httpMethod} is not supported.")
        };
    }
#endif

    private static OpenApiSchema CreateOpenApiSchemaForType(IType type, ISchemaDefinition schemaDefinition)
    {
        if (type.IsListType())
        {
            var elementType = type.ElementType();
            OpenApiSchemaAbstraction itemSchema = CreateOpenApiSchemaForType(elementType, schemaDefinition);

            itemSchema = ApplyNullability(itemSchema, elementType);

            return CreateArraySchema(itemSchema);
        }

        var namedType = type.NamedType();

        if (namedType is IScalarTypeDefinition scalarType)
        {
            return CreateScalarSchema(scalarType);
        }

        if (namedType is IEnumTypeDefinition enumType)
        {
            return CreateEnumSchema(enumType);
        }

        if (namedType is IObjectTypeDefinition objectType)
        {
            var schema = CreateObjectSchema();

            foreach (var field in objectType.Fields)
            {
                if (field.IsIntrospectionField)
                {
                    continue;
                }

                var fieldTypeSchema = CreateOpenApiSchemaForType(field.Type, schemaDefinition);
                fieldTypeSchema.Deprecated = field.IsDeprecated;

                if (field.Description is not null)
                {
                    fieldTypeSchema.Description = field.Description;
                }

                schema.Properties!.Add(field.Name, ApplyNullability(fieldTypeSchema, field.Type));
            }

            schema.Description = objectType.Description;

            return schema;
        }

        if (namedType is IInputObjectTypeDefinition inputObject)
        {
            var schema = CreateObjectSchema();

            foreach (var field in inputObject.Fields)
            {
                if (field.IsIntrospectionField)
                {
                    continue;
                }

                if (field.Type.IsNonNullType())
                {
                    schema.Required!.Add(field.Name);
                }

                var fieldTypeSchema = CreateOpenApiSchemaForType(field.Type, schemaDefinition);
                fieldTypeSchema.Deprecated = field.IsDeprecated;

                if (field.Description is not null)
                {
                    fieldTypeSchema.Description = field.Description;
                }

                schema.Properties!.Add(field.Name, fieldTypeSchema);
            }

            schema.Description = inputObject.Description;

            return schema;
        }

        if (namedType is IInterfaceTypeDefinition interfaceType)
        {
            var possibleTypes = schemaDefinition.GetPossibleTypes(interfaceType);
            var items = new List<OpenApiSchemaAbstraction>();

            foreach (var possibleType in possibleTypes)
            {
                var typeSchema = CreateOpenApiSchemaForType(possibleType, schemaDefinition);

                items.Add(typeSchema);
            }

            var schema = CreateOneOfSchema(items);
            schema.Description = interfaceType.Description;

            return schema;
        }

        if (namedType is IUnionTypeDefinition unionType)
        {
            var items = new List<OpenApiSchemaAbstraction>();

            foreach (var possibleType in unionType.Types)
            {
                var typeSchema = CreateOpenApiSchemaForType(possibleType, schemaDefinition);

                items.Add(typeSchema);
            }

            var schema = CreateOneOfSchema(items);
            schema.Description = unionType.Description;

            return schema;
        }

        throw new NotSupportedException();
    }

    private static OpenApiSchema CreateOneOfSchema(IList<OpenApiSchemaAbstraction> schemas)
    {
        return new OpenApiSchema { OneOf = schemas };
    }

    private static OpenApiSchema CreateAllOfSchema(IList<OpenApiSchemaAbstraction> schemas)
    {
        return new OpenApiSchema { AllOf = schemas };
    }

    private static OpenApiSchema CreateObjectSchema()
    {
        var schema = new OpenApiSchema();

#if NET10_0_OR_GREATER
        schema.Type = JsonSchemaType.Object;
#else
        schema.Type = "object";
#endif
        schema.Properties = new Dictionary<string, OpenApiSchemaAbstraction>();
        schema.Required = new HashSet<string>();

        return schema;
    }

    private static OpenApiSchema CreateArraySchema(OpenApiSchemaAbstraction itemSchema)
    {
        var schema = new OpenApiSchema();
#if NET10_0_OR_GREATER
        schema.Type = JsonSchemaType.Array;
#else
        schema.Type = "array";
#endif
        schema.Items = itemSchema;

        return schema;
    }

    private static OpenApiSchema CreateEnumSchema(IEnumTypeDefinition enumType)
    {
        var schema = new OpenApiSchema { Description = enumType.Description };

#if NET10_0_OR_GREATER
        schema.Type = JsonSchemaType.String;
        schema.Enum = enumType.Values.Select(JsonNode (v) => JsonValue.Create(v.Name)).ToList();
#else
        schema.Type = "string";
        schema.Enum = enumType.Values.Select(IOpenApiAny (v) => new OpenApiString(v.Name)).ToList();
#endif

        return schema;
    }

    private static OpenApiSchema CreateScalarSchema(IScalarTypeDefinition scalarType)
    {
        var schema = new OpenApiSchema();

        if (scalarType.Name is not "ID" and not "String" and not "Int" and not "Float" and not "Boolean")
        {
            schema.Description = scalarType.Description;
        }

#if NET10_0_OR_GREATER
        var jsonSchemaTypes = GetJsonSchemaTypes(scalarType);

        if (jsonSchemaTypes.Count == 1)
        {
            schema.Type = jsonSchemaTypes[0];

            if (schema.Type == JsonSchemaType.String && !string.IsNullOrEmpty(scalarType.Pattern))
            {
                schema.Pattern = scalarType.Pattern;
            }
        }
        else
        {
            schema.OneOf = jsonSchemaTypes
                .Select(IOpenApiSchema (t) => new OpenApiSchema { Type = t })
                .ToList();
        }
#else
        var jsonSchemaTypes = GetJsonSchemaTypes(scalarType);

        if (jsonSchemaTypes.Count == 1)
        {
            schema.Type = jsonSchemaTypes[0];

            if (schema.Type == "string" && !string.IsNullOrEmpty(scalarType.Pattern))
            {
                schema.Pattern = scalarType.Pattern;
            }
        }
        else
        {
            schema.OneOf = jsonSchemaTypes
                .Select(t => new OpenApiSchema { Type = t })
                .ToList();
        }
#endif

        schema.Format = GetJsonSchemaFormat(scalarType);
        schema.Pattern = GetJsonSchemaPattern(scalarType);

        return schema;
    }

    private static string? GetJsonSchemaFormat(IScalarTypeDefinition scalarType)
    {
        var format = scalarType.SpecifiedBy?.OriginalString switch
        {
            "https://scalars.graphql.org/andimarek/date-time.html" => "date-time",
            "https://scalars.graphql.org/andimarek/local-date.html" => "date",
            _ => null
        };

        if (format is null)
        {
            var serializationType = scalarType.GetScalarSerializationType();

            return serializationType switch
            {
                ScalarSerializationType.Int => "int32",
                ScalarSerializationType.Float => "float",
                _ => null
            };
        }

        return format;
    }

    private static string? GetJsonSchemaPattern(IScalarTypeDefinition scalarType)
    {
        var pattern = scalarType.SpecifiedBy?.OriginalString switch
        {
            "https://scalars.graphql.org/andimarek/date-time.html"
                => @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?(?:[Zz]|[+-]\d{2}:\d{2})$",
            "https://scalars.graphql.org/andimarek/local-date.html"
                => @"^\d{4}-\d{2}-\d{2}$",
            _ => null
        };

        return pattern ?? scalarType.Pattern;
    }

#if NET10_0_OR_GREATER
    private static List<JsonSchemaType> GetJsonSchemaTypes(IScalarTypeDefinition scalarType)
    {
        var serializationType = scalarType.GetScalarSerializationType();

        var possibleTypes = new List<JsonSchemaType>();

        if (serializationType.HasFlag(ScalarSerializationType.String))
        {
            possibleTypes.Add(JsonSchemaType.String);
        }

        if (serializationType.HasFlag(ScalarSerializationType.Boolean))
        {
            possibleTypes.Add(JsonSchemaType.Boolean);
        }

        if (serializationType.HasFlag(ScalarSerializationType.Int))
        {
            possibleTypes.Add(JsonSchemaType.Integer);
        }

        if (serializationType.HasFlag(ScalarSerializationType.Float))
        {
            possibleTypes.Add(JsonSchemaType.Number);
        }

        if (serializationType.HasFlag(ScalarSerializationType.Object))
        {
            possibleTypes.Add(JsonSchemaType.Object);
        }

        if (serializationType.HasFlag(ScalarSerializationType.List))
        {
            possibleTypes.Add(JsonSchemaType.Array);
        }

        if (possibleTypes.Count > 0)
        {
            return possibleTypes;
        }

        return [JsonSchemaType.String];
    }
#else
    private static List<string> GetJsonSchemaTypes(IScalarTypeDefinition scalarType)
    {
        var serializationType = scalarType.GetScalarSerializationType();

        var possibleTypes = new List<string>();

        if (serializationType.HasFlag(ScalarSerializationType.String))
        {
            possibleTypes.Add("string");
        }

        if (serializationType.HasFlag(ScalarSerializationType.Boolean))
        {
            possibleTypes.Add("boolean");
        }

        if (serializationType.HasFlag(ScalarSerializationType.Int))
        {
            possibleTypes.Add("integer");
        }

        if (serializationType.HasFlag(ScalarSerializationType.Float))
        {
            possibleTypes.Add("number");
        }

        if (serializationType.HasFlag(ScalarSerializationType.Object))
        {
            possibleTypes.Add("object");
        }

        if (serializationType.HasFlag(ScalarSerializationType.List))
        {
            possibleTypes.Add("array");
        }

        if (possibleTypes.Count > 0)
        {
            return possibleTypes;
        }

        return ["string"];
    }
#endif

    private static OpenApiSchemaAbstraction CreateOpenApiSchemaForSelectionSet(
        SelectionSetNode selectionSet,
        IOutputType typeDefinition,
        ISchemaDefinition schemaDefinition,
        Dictionary<string, FragmentDefinitionNode> fragmentLookup,
        Dictionary<string, OpenApiFragmentDocument> externalFragmentLookup,
        bool optional = false)
    {
        if (typeDefinition.IsListType())
        {
            var elementType = (IOutputType)typeDefinition.ElementType();
            var itemSchema = CreateOpenApiSchemaForSelectionSet(
                selectionSet,
                elementType,
                schemaDefinition,
                fragmentLookup,
                externalFragmentLookup,
                optional);

            itemSchema = ApplyNullability(itemSchema, elementType);

            return CreateArraySchema(itemSchema);
        }

        var namedType = typeDefinition.NamedType();

        OpenApiSchema? fieldSchema = null;
        Dictionary<string, List<OpenApiSchemaAbstraction>>? fragmentSchemasByType = null;

        foreach (var selection in selectionSet.Selections)
        {
            var isSelectionConditional = IsConditional(selection);

            if (selection is FieldNode field)
            {
                var fieldName = field.Name.Value;
                var responseName = field.Alias?.Value ?? fieldName;

                var fieldType = fieldName == IntrospectionFieldNames.TypeName ?
                    new NonNullType(schemaDefinition.Types["String"]) :
                    ((IComplexTypeDefinition)namedType).Fields[fieldName].Type;

                OpenApiSchemaAbstraction typeSchema;
                if (field.SelectionSet is not null)
                {
                    typeSchema = CreateOpenApiSchemaForSelectionSet(
                        field.SelectionSet,
                        fieldType,
                        schemaDefinition,
                        fragmentLookup,
                        externalFragmentLookup);
                }
                else
                {
                    typeSchema = CreateOpenApiSchemaForType(fieldType, schemaDefinition);
                }

                fieldSchema ??= CreateObjectSchema();

                if (!optional && !isSelectionConditional)
                {
                    fieldSchema.Required!.Add(responseName);
                }

                typeSchema = ApplyNullability(typeSchema, fieldType);

                fieldSchema.Properties!.Add(responseName, typeSchema);
            }
            else if (selection is InlineFragmentNode inlineFragment)
            {
                var typeCondition = inlineFragment.TypeCondition is null
                    ? namedType
                    : schemaDefinition.Types[inlineFragment.TypeCondition.Name.Value];
                var isDifferentType = !typeCondition.IsAssignableFrom(namedType);
                var typeName = typeCondition.Name;

                var typeConditionSchema = CreateOpenApiSchemaForSelectionSet(
                    inlineFragment.SelectionSet,
                    (IOutputType)typeCondition,
                    schemaDefinition,
                    fragmentLookup,
                    externalFragmentLookup,
                    optional: optional || isDifferentType || isSelectionConditional);

                fragmentSchemasByType ??= new Dictionary<string, List<OpenApiSchemaAbstraction>>();

                if (!fragmentSchemasByType.TryGetValue(typeName, out var schemaList))
                {
                    schemaList = [];
                    fragmentSchemasByType[typeName] = schemaList;
                }

                schemaList.Add(typeConditionSchema);
            }
            else if (selection is FragmentSpreadNode fragmentSpread)
            {
                var fragmentName = fragmentSpread.Name.Value;

                if (externalFragmentLookup.TryGetValue(fragmentName, out var externalFragment))
                {
                    var typeName = externalFragment.TypeCondition.Name;

                    fragmentSchemasByType ??= new Dictionary<string, List<OpenApiSchemaAbstraction>>();

                    if (!fragmentSchemasByType.TryGetValue(typeName, out var schemaList))
                    {
                        schemaList = [];
                        fragmentSchemasByType[typeName] = schemaList;
                    }

#if NET10_0_OR_GREATER
                    schemaList.Add(new OpenApiSchemaReference(fragmentName));
#else
                    var externalReference = new OpenApiSchema
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.Schema,
                            Id = fragmentName
                        }
                    };

                    schemaList.Add(externalReference);
#endif
                }
                else
                {
                    var fragment = fragmentLookup[fragmentName];
                    var typeCondition = schemaDefinition.Types[fragment.TypeCondition.Name.Value];
                    var isDifferentType = !typeCondition.IsAssignableFrom(namedType);
                    var typeName = typeCondition.Name;

                    var typeConditionSchema = CreateOpenApiSchemaForSelectionSet(
                        fragment.SelectionSet,
                        (IOutputType)typeCondition,
                        schemaDefinition,
                        fragmentLookup,
                        externalFragmentLookup,
                        optional: optional || isDifferentType || isSelectionConditional);

                    fragmentSchemasByType ??= new Dictionary<string, List<OpenApiSchemaAbstraction>>();

                    if (!fragmentSchemasByType.TryGetValue(typeName, out var schemaList))
                    {
                        schemaList = [];
                        fragmentSchemasByType[typeName] = schemaList;
                    }

                    schemaList.Add(typeConditionSchema);
                }
            }
        }

        // Build fragment schemas - one entry per type (using allOf to combine multiple fragments on same type)
        var fragmentSchemas = fragmentSchemasByType?
            .Select(kvp => kvp.Value.Count == 1 ? kvp.Value[0] : CreateAllOfSchema(kvp.Value))
            .ToList();

        // Determine the final schema based on what we have
        return (fieldSchema, fragmentSchemas) switch
        {
            // Only field schema, no fragments
            (not null, null or { Count: 0 }) => fieldSchema,

            // Only one fragment schema, no field schema
            (null, { Count: 1 }) => fragmentSchemas[0],

            // Multiple fragment schemas (oneOf), no field schema
            (null, { Count: > 1 }) => CreateOneOfSchema(fragmentSchemas),

            // Field schema + multiple fragment schemas (allOf with oneOf)
            (not null, { Count: > 1 }) => CreateAllOfSchema([fieldSchema, CreateOneOfSchema(fragmentSchemas)]),

            // Field schema + single fragment schema (allOf)
            (not null, { Count: 1 }) => CreateAllOfSchema([fieldSchema, fragmentSchemas[0]]),

            // Should not happen, but handle gracefully
            _ => CreateObjectSchema()
        };
    }

    private static OpenApiSchemaAbstraction ApplyNullability(OpenApiSchemaAbstraction schema, IType type)
    {
        if (!type.IsNonNullType())
        {
#if NET10_0_OR_GREATER
            if (schema is OpenApiSchemaReference schemaReference)
            {
                return CreateAllOfSchema([
                    schemaReference,
                    new OpenApiSchema { Type = JsonSchemaType.Null }
                ]);
            }

            if (schema is OpenApiSchema objectSchema)
            {
                objectSchema.Type |= JsonSchemaType.Null;
            }
#else
            if (schema.Reference is not null)
            {
                return CreateAllOfSchema([
                    schema,
                    new OpenApiSchema { Type = "null" }
                ]);
            }

            schema.Nullable = true;
#endif
        }

        return schema;
    }

    private static OpenApiParameter CreateOpenApiParameter(
        OpenApiOperationDocument operation,
        OpenApiRouteSegmentParameter parameter,
        ParameterLocation location,
        ISchemaDefinition schema)
    {
        var graphqlType = GetGraphQLType(operation.OperationDefinition, parameter, schema);

        return new OpenApiParameter
        {
            Name = parameter.Key,
            In = location,
            Required = graphqlType.IsNonNullType(),
            Schema = CreateOpenApiSchemaForType(graphqlType, schema)
        };
    }

    private static bool IsConditional(ISelectionNode selection)
    {
        if (selection is FieldNode field)
        {
            return HasConditionalDirective(field.Directives);
        }

        if (selection is InlineFragmentNode inlineFragment)
        {
            return HasConditionalDirective(inlineFragment.Directives);
        }

        if (selection is FragmentSpreadNode fragmentSpread)
        {
            return HasConditionalDirective(fragmentSpread.Directives);
        }

        return false;
    }

    private static bool HasConditionalDirective(IReadOnlyList<DirectiveNode> directives)
    {
        foreach (var directive in directives)
        {
            if (directive.Name.Value is "skip" or "include")
            {
                return true;
            }
        }

        return false;
    }

    private static IType GetGraphQLType(
        OperationDefinitionNode operation,
        OpenApiRouteSegmentParameter parameter,
        ISchemaDefinition schema)
    {
        var variable = operation.VariableDefinitions
            .FirstOrDefault(v => v.Variable.Name.Value == parameter.VariableName);

        if (variable is null)
        {
            throw new InvalidOperationException($"Expected to find variable named '{parameter.VariableName}'.");
        }

        var namedVariableType = variable.Type.NamedType().Name.Value;

        if (!schema.Types.TryGetType<IInputType>(variable.Type, out var variableType))
        {
            throw new InvalidOperationException($"Expected to find type '{namedVariableType}'.");
        }

        if (parameter.InputObjectPath is not { } inputObjectPath)
        {
            return variableType;
        }

        var currentType = variableType.NamedType();
        IInputValueDefinition? lastField = null;

        foreach (var segment in inputObjectPath)
        {
            if (currentType is not IInputObjectTypeDefinition inputObject)
            {
                throw new InvalidOperationException($"Expected '{currentType.Name}' to be an input object type.");
            }

            if (!inputObject.Fields.TryGetField(segment, out lastField))
            {
                throw new InvalidOperationException(
                    $"Expected type '{currentType.Name}' to have a field named '{segment}'.");
            }

            currentType = lastField.Type.NamedType();
        }

        return lastField!.Type;
    }

    private sealed record OperationDescriptor(string Path, string HttpMethod, OpenApiOperation Operation);

    private sealed record ComponentDescriptor(string SchemaName, OpenApiSchemaAbstraction Schema);

    private sealed class InputObjectPathTrie : Dictionary<string, InputObjectPathTrie>
    {
        public bool IsTerminal { get; private set; }

        public void Add(OpenApiRouteSegmentParameter parameter)
        {
            if (parameter.InputObjectPath is not { } inputObjectPath || inputObjectPath.Length == 0)
            {
                return;
            }

            var currentNode = this;

            foreach (var segment in inputObjectPath)
            {
                if (!currentNode.TryGetValue(segment, out var nextNode))
                {
                    nextNode = new InputObjectPathTrie();
                    currentNode[segment] = nextNode;
                }

                currentNode = nextNode;
            }

            currentNode.IsTerminal = true;
        }
    }

    private static void RemovePropertiesFromSchema(
        OpenApiSchemaAbstraction schema,
        InputObjectPathTrie trie)
    {
        if (schema.Properties == null || schema.Properties.Count == 0)
        {
            return;
        }

        var propertiesToRemove = new List<string>();

        foreach (var (propertyName, propertySchema) in schema.Properties)
        {
            if (!trie.TryGetValue(propertyName, out var childTrie))
            {
                continue;
            }

            if (childTrie.IsTerminal)
            {
                propertiesToRemove.Add(propertyName);
            }
            else if (childTrie.Count > 0 && propertySchema.Properties != null)
            {
                RemovePropertiesFromSchema(propertySchema, childTrie);
            }
        }

        foreach (var propertyName in propertiesToRemove)
        {
            schema.Properties.Remove(propertyName);
        }

        if (schema.Required != null)
        {
            foreach (var propertyName in propertiesToRemove)
            {
                schema.Required.Remove(propertyName);
            }
        }
    }
}
