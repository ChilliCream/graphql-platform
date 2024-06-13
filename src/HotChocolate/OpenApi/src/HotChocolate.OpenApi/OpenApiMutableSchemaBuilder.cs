using System.Net.Mime;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.OpenApi.Extensions;
using HotChocolate.OpenApi.Helpers;
using HotChocolate.Types;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using OperationType = Microsoft.OpenApi.Models.OperationType;

namespace HotChocolate.OpenApi;

internal sealed class OpenApiMutableSchemaBuilder
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string DateTimeFormat = @"yyyy-MM-ddTHH\:mm\:ss.fffzzz";

    private static readonly Skimmed.ScalarType JsonType = new(ScalarNames.JSON);

    private readonly Skimmed.Schema _skimmedSchema = new();
    private readonly Dictionary<string, OpenApiOperationWrapper> _wrappedOperations = [];

    private readonly OpenApiDocument _openApiDocument;
    private readonly string _httpClientName;
    private bool _mutationConventionsEnabled;
    private MutationConventionOptions _mutationConventionOptions;

    public static OpenApiMutableSchemaBuilder Create(
        OpenApiDocument openApiDocument,
        string httpClientName)
    {
        return new OpenApiMutableSchemaBuilder(openApiDocument, httpClientName);
    }

    private OpenApiMutableSchemaBuilder(OpenApiDocument openApiDocument, string httpClientName)
    {
        _openApiDocument = openApiDocument;
        _httpClientName = httpClientName;
    }

    public OpenApiMutableSchemaBuilder AddMutationConventions(
        MutationConventionOptions options = default)
    {
        _mutationConventionsEnabled = true;
        _mutationConventionOptions = options;

        return this;
    }

    public Skimmed.Schema Build()
    {
        WrapOperations();

        _skimmedSchema.QueryType = CreateQueryType();
        _skimmedSchema.MutationType = CreateMutationType();

        // Remove empty query type.
        if (_skimmedSchema.QueryType.Fields.Count is 0)
        {
            _skimmedSchema.QueryType = null;
        }

        // Remove empty mutation type.
        if (_skimmedSchema.MutationType.Fields.Count is 0)
        {
            _skimmedSchema.MutationType = null;
        }

        return _skimmedSchema;
    }

    private void WrapOperations()
    {
        foreach (var (path, pathItem) in _openApiDocument.Paths)
        {
            foreach (var (operationType, operation) in pathItem.Operations)
            {
                var operationWrapper = new OpenApiOperationWrapper(operation, operationType, path);

                _wrappedOperations.Add(operationWrapper.OperationId, operationWrapper);
            }
        }
    }

    private Skimmed.ObjectType CreateQueryType()
    {
        var queryType = new Skimmed.ObjectType(OperationTypeNames.Query);

        foreach (var (operationId, operationWrapper) in _wrappedOperations)
        {
            if (operationWrapper.Type is not OperationType.Get)
            {
                continue;
            }

            var operation = operationWrapper.Operation;
            var operationName = GraphQLNamingHelper.CreateName(operationId);

            var queryField = new Skimmed.OutputField(operationName.FirstCharacterToLower())
            {
                Description = operation.Description ?? operation.Summary,
                IsDeprecated = operation.Deprecated,
                Type = CreateGraphQLTypeFromOpenApiOperation(operationWrapper, operationName),
            };

            AddArguments(queryField, operationWrapper);
            AddResolver(queryField, operationWrapper);
            AddTagDirectives(queryField, operation.Tags);

            queryType.Fields.Add(queryField);
        }

        return queryType;
    }

    private Skimmed.IType CreateGraphQLTypeFromOpenApiOperation(
        OpenApiOperationWrapper operationWrapper,
        string? operationName = null)
    {
        var typeMap = GetGraphQLTypesFromOpenApiOperation(operationWrapper);

        operationName ??= GraphQLNamingHelper.CreateName(operationWrapper.OperationId);

        return GetSingleType(
            typeMap,
            unionName: GraphQLNamingHelper.CreateOperationResultName(operationName));
    }

    private Skimmed.ObjectType CreateMutationType()
    {
        var mutationType = new Skimmed.ObjectType(OperationTypeNames.Mutation);

        foreach (var (operationId, operationWrapper) in _wrappedOperations)
        {
            if (operationWrapper.Type is OperationType.Get)
            {
                continue;
            }

            var operation = operationWrapper.Operation;
            var operationName = GraphQLNamingHelper.CreateName(operationId);

            var mutationField = new Skimmed.OutputField(operationName.FirstCharacterToLower())
            {
                Description = operation.Description ?? operation.Summary,
                IsDeprecated = operation.Deprecated,
                Type = CreateMutationFieldType(operationWrapper, operationName),
            };

            AddArguments(mutationField, operationWrapper);
            AddResolver(mutationField, operationWrapper);
            AddTagDirectives(mutationField, operation.Tags);

            mutationType.Fields.Add(mutationField);
        }

        return mutationType;
    }

    private Skimmed.IType CreateMutationFieldType(
        OpenApiOperationWrapper operationWrapper,
        string operationName)
    {
        var typeMap = GetGraphQLTypesFromOpenApiOperation(operationWrapper);

        return _mutationConventionsEnabled
            ? CreatePayloadType(operationName, typeMap)
            : GetSingleType(
                typeMap,
                unionName: GraphQLNamingHelper.CreateOperationResultName(operationName));
    }

    private Skimmed.NonNullType CreatePayloadType(
        string operationName,
        Dictionary<string, Skimmed.IType> typeMap)
    {
        var payloadType = new Skimmed.ObjectType(
            GraphQLNamingHelper.CreatePayloadTypeName(operationName, _mutationConventionOptions));

        var successType = GetSingleType(
            typeMap.Where(kv => kv.Key.StartsWith('2')).ToDictionary(kv => kv.Key, kv => kv.Value),
            unionName: GraphQLNamingHelper.CreateOperationResultName(operationName));

        var successTypeName = JsonNamingPolicy.CamelCase.ConvertName(
            Skimmed.TypeExtensions.NamedType(successType).Name);

        payloadType.Fields.Add(new Skimmed.OutputField(successTypeName)
        {
            ContextData = { { WellKnownContextData.OpenApiUseParentResult, true } },
            Type = successType,
        });

        // Errors
        var nonSuccessTypes = typeMap
            .Where(kv => !kv.Key.StartsWith('2')) // Not a 2XX status code.
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        if (nonSuccessTypes.Count is not 0)
        {
            var errorTypeName = GraphQLNamingHelper.CreatePayloadErrorTypeName(
                operationName,
                _mutationConventionOptions);

            var payloadErrorsFieldName = _mutationConventionOptions.PayloadErrorsFieldName ??
                MutationConventionOptionDefaults.PayloadErrorsFieldName;

            var payloadErrorsField =
                new Skimmed.OutputField(payloadErrorsFieldName)
                {
                    ContextData = { { WellKnownContextData.OpenApiIsErrorsField, true } },
                    Type = new Skimmed.ListType(
                        new Skimmed.NonNullType(
                            GetUnionType(nonSuccessTypes, name: errorTypeName))),
                };

            payloadType.Fields.Add(payloadErrorsField);
        }

        _skimmedSchema.Types.Add(payloadType);

        return new Skimmed.NonNullType(payloadType);
    }

    private Skimmed.IType GetSingleType(
        Dictionary<string, Skimmed.IType> typeMap,
        string unionName)
    {
        return typeMap.Count is 1
            ? typeMap.First().Value
            : new Skimmed.NonNullType(GetUnionType(typeMap, unionName));
    }

    private Skimmed.UnionType GetUnionType(
        Dictionary<string, Skimmed.IType> typeMap,
        string name)
    {
        // Return existing union type when available.
        if (_skimmedSchema.Types.TryGetType(name, out var existingType) &&
            existingType is Skimmed.UnionType existingUnionType)
        {
            return existingUnionType;
        }

        var memberTypesMap = typeMap.ToDictionary(kv => kv.Key, kv => CreateUnionMember(kv.Value));

        var unionType = new Skimmed.UnionType(name);

        foreach (var objectType in memberTypesMap.Values)
        {
            unionType.Types.Add(objectType);
        }

        unionType.ContextData.Add(
            WellKnownContextData.OpenApiTypeMap,
            memberTypesMap.ToDictionary(t => t.Key, t => t.Value.Name));

        _skimmedSchema.Types.Add(unionType);

        return unionType;
    }

    private Skimmed.ObjectType CreateUnionMember(Skimmed.IType type)
    {
        if (Skimmed.TypeExtensions.InnerType(type) is Skimmed.ObjectType objectType)
        {
            return objectType;
        }

        var objectTypeName = GraphQLNamingHelper.CreateObjectWrapperTypeName(type);

        // Return existing object type when available.
        if (_skimmedSchema.Types.TryGetType(objectTypeName, out var existingType) &&
            existingType is Skimmed.ObjectType existingObjectType)
        {
            return existingObjectType;
        }

        // Other types need to be wrapped in an object type.
        objectType = new Skimmed.ObjectType(objectTypeName);

        objectType.Fields.Add(new Skimmed.OutputField(WellKnownFieldNames.Value)
        {
            ContextData = { { WellKnownContextData.OpenApiUseParentResult, true } },
            Type = type,
        });

        _skimmedSchema.Types.Add(objectType);

        return objectType;
    }

    private void AddArguments(
        Skimmed.OutputField outputField,
        OpenApiOperationWrapper operationWrapper)
    {
        var operation = operationWrapper.Operation;

        // Parameters
        foreach (var parameter in operation.Parameters)
        {
            if (parameter.Schema is not null)
            {
                var parameterName = GraphQLNamingHelper.CreateName(parameter.Name);

                outputField.Arguments.Add(new Skimmed.InputField(parameterName)
                {
                    DefaultValue = CreateValueNodeFromOpenApiAny(parameter.Schema.Default),
                    Description = parameter.Description,
                    IsDeprecated = parameter.Deprecated,
                    Type = CreateGraphQLTypeFromOpenApiSchema(
                        parameter.Schema,
                        parameter.Schema.Title ??
                            $"{operationWrapper.OperationId} parameter {parameterName}",
                        isInput: true,
                        required: parameter.Required),
                });
            }
        }

        // Request body
        if (operation.RequestBody is not null)
        {
            var openApiMediaTypeObject = operation.RequestBody.Content.SingleOrDefault(
                c => c.Key is MediaTypeNames.Application.Json).Value;

            if (openApiMediaTypeObject is null)
            {
                return;
            }

            var inputTypeName = _mutationConventionsEnabled
                ? GraphQLNamingHelper.CreateInputTypeName(
                    outputField.Name,
                    _mutationConventionOptions)
                : null;

            var inputType = CreateGraphQLTypeFromOpenApiSchema(
                openApiMediaTypeObject.Schema,
                openApiMediaTypeObject.Schema.Title ?? $"{operationWrapper.OperationId} input",
                typeName: inputTypeName,
                required: operation.RequestBody.Required,
                isInput: true);

            var inputArgumentName = _mutationConventionOptions.InputArgumentName ??
                MutationConventionOptionDefaults.InputArgumentName;

            var inputFieldName = _mutationConventionsEnabled
                ? inputArgumentName
                : Skimmed.TypeExtensions.NamedType(inputType).Name.FirstCharacterToLower();

            var inputField = new Skimmed.InputField(inputFieldName)
            {
                Description = operation.RequestBody.Description,
                Type = inputType,
            };

            outputField.ContextData.Add(WellKnownContextData.OpenApiInputFieldName, inputFieldName);

            outputField.Arguments.Add(inputField);
        }
    }

    private void AddResolver(IHasContextData field, OpenApiOperationWrapper operationWrapper)
    {
        field.ContextData[WellKnownContextData.OpenApiResolver] =
            OpenApiResolverFactory.CreateResolver(_httpClientName, operationWrapper);
    }

    private static void AddTagDirectives(
        Skimmed.IDirectivesProvider outputField,
        IEnumerable<OpenApiTag> tags)
    {
        foreach (var tag in tags)
        {
            var tagName = GraphQLNamingHelper.CreateName(tag.Name);

            outputField.Directives.Add(new Skimmed.Directive(
                new Skimmed.DirectiveType(WellKnownDirectives.Tag),
                [new Skimmed.Argument(WellKnownDirectives.Name, tagName)]));
        }
    }

    private static IValueNode? CreateValueNodeFromOpenApiAny(IOpenApiAny openApiAny)
    {
        return openApiAny switch
        {
            OpenApiBoolean b => new BooleanValueNode(b.Value),
            OpenApiDate d => new StringValueNode(d.Value.ToString(DateFormat)),
            OpenApiDateTime d => new StringValueNode(d.Value.ToString(DateTimeFormat)),
            OpenApiDouble d => new FloatValueNode(d.Value),
            OpenApiInteger i => new IntValueNode(i.Value),
            OpenApiFloat f => new FloatValueNode(f.Value),
            OpenApiLong l => new IntValueNode(l.Value),
            OpenApiString s => new StringValueNode(s.Value),
            _ => null,
        };
    }

    private Dictionary<string, Skimmed.IType> GetGraphQLTypesFromOpenApiOperation(
        OpenApiOperationWrapper operationWrapper)
    {
        var typeMap = new Dictionary<string, Skimmed.IType>();

        foreach (var (httpStatusCode, response) in operationWrapper.Operation.Responses)
        {
            if (response.Content.Count is 0)
            {
                // If no response content is defined, then use a (schemaless) JSON type.
                typeMap.Add(httpStatusCode, JsonType);
            }
            else
            {
                foreach (var (mediaType, mediaTypeObject) in response.Content)
                {
                    if (mediaType is MediaTypeNames.Application.Json)
                    {
                        typeMap.Add(
                            httpStatusCode,
                            CreateGraphQLTypeFromOpenApiSchema(
                                mediaTypeObject.Schema,
                                mediaTypeObject.Schema.Title ??
                                    $"{operationWrapper.OperationId} response {httpStatusCode}"));
                    }
                }
            }

            AddLinks(response, typeMap);
        }

        return typeMap;
    }

    private Skimmed.IType CreateGraphQLTypeFromOpenApiSchema(
        OpenApiSchema openApiSchema,
        string schemaTitle,
        string? typeName = null,
        bool isInput = false,
        bool required = true)
    {
        if (openApiSchema.AllOf.Count is not 0)
        {
            openApiSchema = MergeAllOf(openApiSchema);
        }

        typeName ??= GetGraphQLTypeName(openApiSchema, schemaTitle);

        // Return existing type when available.
        if (_skimmedSchema.Types.TryGetType(typeName, out var existingType))
        {
            return openApiSchema.Nullable ? existingType : new Skimmed.NonNullType(existingType);
        }

        Skimmed.IType type;

        switch (openApiSchema.Type)
        {
            case JsonSchemaTypes.Array:
                var elementType = CreateGraphQLTypeFromOpenApiSchema(
                    openApiSchema.Items,
                    openApiSchema.Items.Title ?? $"{schemaTitle} item",
                    required: true);

                type = new Skimmed.ListType(elementType);
                break;

            case JsonSchemaTypes.Boolean:
            case JsonSchemaTypes.Integer:
            case JsonSchemaTypes.Number:
            case JsonSchemaTypes.String:
                var scalarType = new Skimmed.ScalarType(typeName)
                {
                    Description = openApiSchema.Description,
                };

                type = scalarType;
                break;

            case JsonSchemaTypes.Object when isInput:
                var inputObjectType = new Skimmed.InputObjectType(typeName)
                {
                    Description = openApiSchema.Description,
                };

                foreach (var (propertyName, propertySchema) in openApiSchema.Properties)
                {
                    inputObjectType.Fields.Add(
                        CreateInputField(
                            schemaTitle,
                            propertyName,
                            propertySchema,
                            required: openApiSchema.Required.Contains(propertyName)));
                }

                _skimmedSchema.Types.Add(inputObjectType);

                type = inputObjectType;
                break;

            case JsonSchemaTypes.Object when !isInput:
                var objectType = new Skimmed.ObjectType(typeName)
                {
                    Description = openApiSchema.Description,
                };

                foreach (var (propertyName, propertySchema) in openApiSchema.Properties)
                {
                    objectType.Fields.Add(
                        CreateOutputField(
                            schemaTitle,
                            propertyName,
                            propertySchema,
                            required: openApiSchema.Required.Contains(propertyName)));
                }

                _skimmedSchema.Types.Add(objectType);

                type = objectType;
                break;

            default:
                throw new InvalidOperationException();
        }

        return required && !openApiSchema.Nullable
            ? new Skimmed.NonNullType(type)
            : type;
    }

    private static OpenApiSchema MergeAllOf(OpenApiSchema openApiSchema)
    {
        var mergedSchema = new OpenApiSchema(openApiSchema)
        {
            AllOf = [],
            Type = JsonSchemaTypes.Object,
        };

        foreach (var allOfSchema in openApiSchema.AllOf)
        {
            // Properties
            if (allOfSchema.Type is JsonSchemaTypes.Object)
            {
                foreach (var (key, value) in allOfSchema.Properties)
                {
                    mergedSchema.Properties[key] = value;
                }
            }

            // Required
            mergedSchema.Required.UnionWith(allOfSchema.Required);
        }

        return mergedSchema;
    }

    private static string GetGraphQLTypeName(OpenApiSchema openApiSchema, string schemaTitle)
    {
        return openApiSchema.Type switch
        {
            JsonSchemaTypes.Boolean => ScalarNames.Boolean,
            JsonSchemaTypes.Integer when openApiSchema.Format is "int64" => ScalarNames.Long,
            JsonSchemaTypes.Integer => ScalarNames.Int,
            JsonSchemaTypes.Number => ScalarNames.Float,
            JsonSchemaTypes.String when openApiSchema.Format is "date" => ScalarNames.Date,
            JsonSchemaTypes.String when openApiSchema.Format is "date-time" => ScalarNames.DateTime,
            JsonSchemaTypes.String => ScalarNames.String,
            _ => GraphQLNamingHelper.CreateTypeName(openApiSchema.Reference?.Id ?? schemaTitle),
        };
    }

    private Skimmed.InputField CreateInputField(
        string schemaTitle,
        string propertyName,
        OpenApiSchema propertySchema,
        bool required = false)
    {
        return new Skimmed.InputField(GraphQLNamingHelper.CreateName(propertyName))
        {
            ContextData =
            {
                [WellKnownContextData.OpenApiPropertyName] = propertyName,
            },
            DefaultValue = CreateValueNodeFromOpenApiAny(propertySchema.Default),
            Description = propertySchema.Description,
            IsDeprecated = propertySchema.Deprecated,
            Type = CreateGraphQLTypeFromOpenApiSchema(
                propertySchema,
                propertySchema.Title ?? $"{schemaTitle} property {propertyName}",
                required: required),
        };
    }

    private Skimmed.OutputField CreateOutputField(
        string schemaTitle,
        string propertyName,
        OpenApiSchema propertySchema,
        bool required = false)
    {
        return new Skimmed.OutputField(GraphQLNamingHelper.CreateName(propertyName))
        {
            ContextData =
            {
                [WellKnownContextData.OpenApiPropertyName] = propertyName,
            },
            Description = propertySchema.Description,
            IsDeprecated = propertySchema.Deprecated,
            Type = CreateGraphQLTypeFromOpenApiSchema(
                propertySchema,
                propertySchema.Title ?? $"{schemaTitle} property {propertyName}",
                required: required),
        };
    }

    private void AddLinks(OpenApiResponse response, Dictionary<string, Skimmed.IType> typeMap)
    {
        foreach (var (linkName, openApiLink) in response.Links)
        {
            if (openApiLink.OperationId is null ||
                !_wrappedOperations.TryGetValue(
                    openApiLink.OperationId,
                    out var linkedOperationWrapper))
            {
                continue;
            }

            var graphQLType = CreateGraphQLTypeFromOpenApiOperation(linkedOperationWrapper);

            foreach (var type in typeMap.Values)
            {
                var innerType = Skimmed.TypeExtensions.InnerType(type);

                List<Skimmed.ObjectType> objectTypes = [];

                switch (innerType)
                {
                    case Skimmed.ObjectType objectType:
                        objectTypes.Add(objectType);
                        break;

                    case Skimmed.ListType listType
                        when Skimmed.TypeExtensions.InnerType(listType.ElementType) is
                            Skimmed.ObjectType objectType:

                        objectTypes.Add(objectType);
                        break;
                }

                foreach (var objectType in objectTypes)
                {
                    var linkField = new Skimmed.OutputField(linkName)
                    {
                        Description = openApiLink.Description,
                        Type = graphQLType,
                    };

                    AddResolver(linkField, linkedOperationWrapper);

                    objectType.Fields.Add(linkField);
                }
            }
        }
    }
}
