using System.Collections.Concurrent;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.AspNetCore.OpenApi;
#if NET10_0_OR_GREATER
using Microsoft.OpenApi;
using System.Text.Json.Nodes;

#else
using Microsoft.OpenApi.Models;
using OperationType = Microsoft.OpenApi.Models.OperationType;
using Microsoft.OpenApi.Any;
#endif

namespace HotChocolate.Exporters.OpenApi;

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
        List<OpenApiOperationDocument> operations,
        List<OpenApiFragmentDocument> fragments,
        ISchemaDefinition schema)
    {
        var operationDescriptors = new List<OperationDescriptor>();
        var componentDescriptors = new List<ComponentDescriptor>();

        foreach (var operation in operations)
        {
            var operationDescriptor = CreateOperationDescriptor(operation, schema);
            operationDescriptors.Add(operationDescriptor);
        }

        foreach (var fragment in fragments)
        {
            var componentDescriptor = CreateComponentDescriptor(fragment, schema);
            componentDescriptors.Add(componentDescriptor);
        }

        _operations = operationDescriptors;
        _components = componentDescriptors;
    }

    private const string JsonContentType = "application/json";

    private static OperationDescriptor CreateOperationDescriptor(
        OpenApiOperationDocument operationDocument,
        ISchemaDefinition schema)
    {
        var operation = new OpenApiOperation
        {
            Description = operationDocument.Description,
            OperationId = GetOperationId(operationDocument.Name),
            Responses = new OpenApiResponses()
        };

#if NET10_0_OR_GREATER
        operation.Parameters = new List<IOpenApiParameter>();
#else
        operation.Parameters = new List<OpenApiParameter>();
#endif

        var bodyVariable = operationDocument.BodyParameter is { } bodyParameter ? bodyParameter.Variable : null;
        var trie = new InputObjectPathTrie();

        foreach (var routeParameter in operationDocument.Route.Parameters)
        {
            var parameter = CreateOpenApiParameter(operationDocument, routeParameter, ParameterLocation.Path, schema);

            operation.Parameters.Add(parameter);

            if (routeParameter.Variable == bodyVariable && routeParameter.InputObjectPath is not null)
            {
                trie.Add(routeParameter);
            }
        }

        foreach (var queryParameter in operationDocument.QueryParameters)
        {
            var parameter = CreateOpenApiParameter(operationDocument, queryParameter, ParameterLocation.Query, schema);

            operation.Parameters.Add(parameter);

            if (queryParameter.Variable == bodyVariable && queryParameter.InputObjectPath is not null)
            {
                trie.Add(queryParameter);
            }
        }

        if (operationDocument.BodyParameter is { } bodyParameter2)
        {
            var graphqlType = GetGraphQLType(operationDocument.OperationDefinition, bodyParameter2, schema);
            var requestBodyType = CreateOpenApiSchemaForType(graphqlType, schema);

            RemovePropertiesFromSchema(requestBodyType, trie);

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

        if (operationDocument.OperationDefinition.SelectionSet.Selections is not
            [FieldNode { SelectionSet: not null } rootField])
        {
            throw new InvalidOperationException("Expected to have a single field selection on the root");
        }

        var fieldType = operationType.Fields[rootField.Name.Value].Type;

        var responseBody = new OpenApiMediaType
        {
            Schema = CreateOpenApiSchemaForSelectionSet(
                rootField.SelectionSet,
                fieldType,
                schema,
                operationDocument.LocalFragmentLookup,
                operationDocument.ExternalFragmentReferences)
        };

        operation.Responses["200"] = new OpenApiResponse
        {
            Content = new ConcurrentDictionary<string, OpenApiMediaType> { [JsonContentType] = responseBody }
        };

        // TODO: Add other response codes

        return new OperationDescriptor(
            operationDocument.Route.ToOpenApiPath(),
            operationDocument.HttpMethod,
            operation);
    }

    private static ComponentDescriptor CreateComponentDescriptor(
        OpenApiFragmentDocument fragmentDocument,
        ISchemaDefinition schema)
    {
        var componentSchema = CreateOpenApiSchemaForSelectionSet(
            fragmentDocument.FragmentDefinition.SelectionSet,
            (IOutputType)fragmentDocument.TypeCondition,
            schema,
            fragmentDocument.LocalFragmentLookup,
            fragmentDocument.ExternalFragmentReferences);

        componentSchema.Description = fragmentDocument.Description;

        return new ComponentDescriptor(fragmentDocument.Name, componentSchema);
    }

    private static void AddComponent(OpenApiDocument document, string schemaName, OpenApiSchema schema)
    {
        document.Components ??= new OpenApiComponents();

#if NET10_0_OR_GREATER
        document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();
#else
        document.Components.Schemas ??= new Dictionary<string, OpenApiSchema>();
#endif

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
            var concretePathItem = new OpenApiPathItem { Operations = new Dictionary<HttpMethod, OpenApiOperation>() };

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
            var graphqlElementType = type.ElementType();
            var itemSchema = CreateOpenApiSchemaForType(graphqlElementType, schemaDefinition);

            return CreateArraySchema(itemSchema);
        }

        var namedType = type.NamedType();

        if (namedType is IScalarTypeDefinition scalarType)
        {
            return CreateScalarSchema(scalarType);
        }

        if (namedType is IEnumTypeDefinition enumType)
        {
            return CreatEnumSchema(enumType);
        }

        var schema = new OpenApiSchema();

        if (namedType is IObjectTypeDefinition objectType)
        {
            schema = CreateObjectSchema();

            foreach (var field in objectType.Fields)
            {
                if (field.Type.IsNonNullType())
                {
                    schema.Required!.Add(field.Name);
                }

                var fieldTypeSchema = CreateOpenApiSchemaForType(field.Type, schemaDefinition);
                fieldTypeSchema.Deprecated = field.IsDeprecated;

                schema.Properties!.Add(field.Name, fieldTypeSchema);
            }
        }
        else if (namedType is IInputObjectTypeDefinition inputObject)
        {
            schema = CreateObjectSchema();

            foreach (var field in inputObject.Fields)
            {
                if (field.Type.IsNonNullType())
                {
                    schema.Required!.Add(field.Name);
                }

                var fieldTypeSchema = CreateOpenApiSchemaForType(field.Type, schemaDefinition);
                fieldTypeSchema.Deprecated = field.IsDeprecated;

                schema.Properties!.Add(field.Name, fieldTypeSchema);
            }
        }
        else if (namedType is IInterfaceTypeDefinition interfaceType)
        {
            var possibleTypes = schemaDefinition.GetPossibleTypes(interfaceType);
            var items = new List<OpenApiSchema>();

            foreach (var possibleType in possibleTypes)
            {
                var typeSchema = CreateOpenApiSchemaForType(possibleType, schemaDefinition);

                items.Add(typeSchema);
            }

            schema = CreateOneOfSchema(items);
        }
        else if (namedType is IUnionTypeDefinition unionType)
        {
            var items = new List<OpenApiSchema>();

            foreach (var possibleType in unionType.Types)
            {
                var typeSchema = CreateOpenApiSchemaForType(possibleType, schemaDefinition);

                items.Add(typeSchema);
            }

            schema = CreateOneOfSchema(items);
        }

        if (namedType is IDescriptionProvider descriptionProvider)
        {
            schema.Description = descriptionProvider.Description;
        }

        return schema;
    }

    private static OpenApiSchema CreateOneOfSchema(IList<OpenApiSchema> schemas)
    {
        var schema = CreateObjectSchema();

#if NET10_0_OR_GREATER
        schema.OneOf = schemas.Cast<IOpenApiSchema>().ToList();
#else
        schema.OneOf = schemas;
#endif

        return schema;
    }

    private static OpenApiSchema CreateObjectSchema()
    {
        var schema = new OpenApiSchema();

#if NET10_0_OR_GREATER
        schema.Type = JsonSchemaType.Object;
        schema.Properties = new Dictionary<string, IOpenApiSchema>();
#else
        schema.Type = "object";
        schema.Properties = new Dictionary<string, OpenApiSchema>();
#endif
        schema.Required = new HashSet<string>();

        return schema;
    }

    private static OpenApiSchema CreateArraySchema(OpenApiSchema itemSchema)
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

    private static OpenApiSchema CreatEnumSchema(IEnumTypeDefinition enumType)
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
        schema.Type = GetJsonSchemaTypeFromScalar(scalarType);
#else
        schema.Type = GetJsonSchemaTypeFromScalar(scalarType);
#endif

        schema.Format = GetFormatFromScalar(scalarType);

        return schema;
    }

    private static string? GetFormatFromScalar(IScalarTypeDefinition scalarType)
    {
        return scalarType.Name switch
        {
            "Int" => "int32",
            "Float" => "float",
            _ => null
        };
    }

#if NET10_0_OR_GREATER
    private static JsonSchemaType GetJsonSchemaTypeFromScalar(IScalarTypeDefinition scalarType)
    {
        return scalarType.Name switch
        {
            "Int" => JsonSchemaType.Integer,
            "Float" => JsonSchemaType.Number,
            "Boolean" => JsonSchemaType.Boolean,
            _ => JsonSchemaType.String
        };
    }
#else
    private static string GetJsonSchemaTypeFromScalar(IScalarTypeDefinition scalarType)
    {
        return scalarType.Name switch
        {
            "Int" => "integer",
            "Float" => "number",
            "Boolean" => "boolean",
            _ => "string"
        };
    }
#endif

    // TODO: We need to handle introspection fields here
    private static OpenApiSchema CreateOpenApiSchemaForSelectionSet(
        SelectionSetNode selectionSet,
        IOutputType typeDefinition,
        ISchemaDefinition schemaDefinition,
        Dictionary<string, FragmentDefinitionNode> fragmentLookup,
        HashSet<string> externalFragments,
        bool optional = false)
    {
        if (typeDefinition.IsListType())
        {
            var elementType = typeDefinition.ElementType();
            var itemSchema = CreateOpenApiSchemaForType(elementType, schemaDefinition);

            return CreateArraySchema(itemSchema);
        }

        var complexType = typeDefinition.NamedType<IComplexTypeDefinition>();

        var schema = CreateObjectSchema();

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is FieldNode field)
            {
                var fieldName = field.Name.Value;
                var responseName = field.Alias?.Value ?? fieldName;

                var fieldType = complexType.Fields[fieldName].Type;

                OpenApiSchema typeSchema;
                if (field.SelectionSet is not null)
                {
                    typeSchema = CreateOpenApiSchemaForSelectionSet(
                        field.SelectionSet,
                        fieldType,
                        schemaDefinition,
                        fragmentLookup,
                        externalFragments);
                }
                else
                {
                    typeSchema = CreateOpenApiSchemaForType(fieldType, schemaDefinition);
                }

                var isNullable = optional || !fieldType.IsNonNullType();

#if NET10_0_OR_GREATER
                if (isNullable)
                {
                    typeSchema.Type |= JsonSchemaType.Null;
                }
#else
                typeSchema.Nullable = isNullable;
#endif

                schema.Properties!.Add(responseName, typeSchema);
            }
            else if (selection is InlineFragmentNode inlineFragment)
            {
                var typeCondition = inlineFragment.TypeCondition is null
                    ? complexType
                    : schemaDefinition.Types[inlineFragment.TypeCondition.Name.Value];
                var isDifferentType = !typeCondition.IsAssignableFrom(complexType);

                var typeConditionSchema = CreateOpenApiSchemaForSelectionSet(
                    inlineFragment.SelectionSet,
                    (IOutputType)typeCondition,
                    schemaDefinition,
                    fragmentLookup,
                    externalFragments,
                    optional: optional || isDifferentType);

#if NET10_0_OR_GREATER
                schema.AllOf ??= new List<IOpenApiSchema>();
#else
                schema.AllOf ??= new List<OpenApiSchema>();
#endif

                schema.AllOf.Add(typeConditionSchema);
            }
            else if (selection is FragmentSpreadNode fragmentSpread)
            {
                var fragmentName = fragmentSpread.Name.Value;

                if (externalFragments.Contains(fragmentName))
                {
#if NET10_0_OR_GREATER
                    schema.AllOf ??= new List<IOpenApiSchema>();
                    schema.AllOf.Add(new OpenApiSchemaReference(fragmentName));
#else
                    var externalReference = new OpenApiSchema();
                    externalReference.Reference = new OpenApiReference { Type = ReferenceType.Schema, Id =
 fragmentName};
                    schema.AllOf ??= new List<OpenApiSchema>();
                    schema.AllOf.Add(externalReference);
#endif
                }
                else
                {
                    var fragment = fragmentLookup[fragmentName];
                    var typeCondition = schemaDefinition.Types[fragment.TypeCondition.Name.Value];
                    var isDifferentType = !typeCondition.IsAssignableFrom(complexType);

                    var typeConditionSchema = CreateOpenApiSchemaForSelectionSet(
                        fragment.SelectionSet,
                        (IOutputType)typeCondition,
                        schemaDefinition,
                        fragmentLookup,
                        externalFragments,
                        optional: optional || isDifferentType);

#if NET10_0_OR_GREATER
                    schema.AllOf ??= new List<IOpenApiSchema>();
#else
                    schema.AllOf ??= new List<OpenApiSchema>();
#endif

                    schema.AllOf.Add(typeConditionSchema);
                }
            }
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

    private static IType GetGraphQLType(
        OperationDefinitionNode operation,
        OpenApiRouteSegmentParameter parameter,
        ISchemaDefinition schema)
    {
        var variable = operation.VariableDefinitions
            .FirstOrDefault(v => v.Variable.Name.Value == parameter.Variable);

        if (variable is null)
        {
            throw new InvalidOperationException($"Expected to find variable named '{parameter.Variable}'.");
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

        ITypeDefinition currentType = variableType.NamedType();
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

    private static string GetOperationId(string input)
    {
        if (char.IsLower(input[0]))
        {
            return input;
        }

        return string.Create(input.Length, input, (span, str) =>
        {
            str.AsSpan().CopyTo(span);
            span[0] = char.ToLowerInvariant(span[0]);
        });
    }

    private sealed record OperationDescriptor(string Path, string HttpMethod, OpenApiOperation Operation);

    private sealed record ComponentDescriptor(string SchemaName, OpenApiSchema Schema);

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
#if NET10_0_OR_GREATER
        IOpenApiSchema schema,
#else
        OpenApiSchema schema,
#endif
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
