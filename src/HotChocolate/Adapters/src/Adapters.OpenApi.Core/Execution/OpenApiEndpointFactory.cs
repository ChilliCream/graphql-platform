#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.Adapters.OpenApi;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal static class OpenApiEndpointFactory
{
    public static Endpoint Create(
        OpenApiEndpointDefinition endpoints,
        IDictionary<string, OpenApiModelDefinition> modelsByName,
        ISchemaDefinition schema)
    {
        var endpointDescriptor = CreateEndpointDescriptor(endpoints, modelsByName, schema);

        return CreateEndpoint(schema.Name, endpointDescriptor);
    }

    public static Endpoint CreateEndpoint(string schemaName, OpenApiEndpointDescriptor endpointDescriptor)
    {
        var requestDelegate = CreateRequestDelegate(schemaName, endpointDescriptor);

        var builder = new RouteEndpointBuilder(
            requestDelegate: requestDelegate,
            routePattern: endpointDescriptor.Route,
            order: 0)
        {
            DisplayName = endpointDescriptor.Route.RawText
        };

        builder.Metadata.Add(new HttpMethodMetadata([endpointDescriptor.HttpMethod]));

        return builder.Build();
    }

    private static RequestDelegate CreateRequestDelegate(
        string schemaName,
        OpenApiEndpointDescriptor endpointDescriptor)
    {
        var middleware = new DynamicEndpointMiddleware(schemaName, endpointDescriptor);
        return middleware.InvokeAsync;
    }

    public static OpenApiEndpointDescriptor CreateEndpointDescriptor(
        OpenApiEndpointDefinition endpointDefinition,
        IDictionary<string, OpenApiModelDefinition> modelsByName,
        ISchemaDefinition schema)
    {
        var hoistedSelection = endpointDefinition.GetHoistedSelection(schema);
        var document = ComposeExecutionDocument(endpointDefinition, modelsByName);

        document = HoistDirectiveRewriter.Instance.Rewrite(document);

        var route = RoutePatternFactory.Parse(endpointDefinition.Route);

        var parameterTrie = new VariableValueInsertionTrie();

        InsertParametersIntoTrie(endpointDefinition.RouteParameters, OpenApiEndpointParameterType.Route);
        InsertParametersIntoTrie(endpointDefinition.QueryParameters, OpenApiEndpointParameterType.Query);

        var documentValidator = schema.Services.GetRequiredService<DocumentValidator>();

        var validationResult = documentValidator.Validate(schema, document);
        var hasValidDocument = !validationResult.HasErrors;

        return new OpenApiEndpointDescriptor(
            document,
            hasValidDocument,
            endpointDefinition.HttpMethod,
            route,
            parameterTrie,
            endpointDefinition.BodyVariableName,
            hoistedSelection);

        void InsertParametersIntoTrie(
            IEnumerable<OpenApiEndpointDefinitionParameter> parameters,
            OpenApiEndpointParameterType parameterType)
        {
            foreach (var parameter in parameters)
            {
                var (inputType, hasDefaultValue, isNonNullType) = GetParameterDetails(
                    parameter,
                    endpointDefinition.OperationDefinition,
                    schema);

                var leaf = new VariableValueInsertionTrieLeaf(
                    parameter.Key,
                    inputType,
                    parameterType,
                    hasDefaultValue,
                    isNonNullType);

                var inputObjectPath = parameter.InputObjectPath;

                if (!inputObjectPath.HasValue)
                {
                    parameterTrie[parameter.VariableName] = leaf;
                }
                else
                {
                    VariableValueInsertionTrie variableTrie;
                    if (!parameterTrie.TryGetValue(parameter.VariableName, out var existingVariableSegment))
                    {
                        variableTrie = [];
                        parameterTrie[parameter.VariableName] = variableTrie;
                    }
                    else if (existingVariableSegment is VariableValueInsertionTrie existingVariableTrie)
                    {
                        variableTrie = existingVariableTrie;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }

                    var currentTrie = variableTrie;
                    var path = inputObjectPath.Value;

                    for (var i = 0; i < path.Length - 1; i++)
                    {
                        var fieldName = path[i];
                        if (!currentTrie.TryGetValue(fieldName, out var existingSegment))
                        {
                            var newTrie = new VariableValueInsertionTrie();
                            currentTrie[fieldName] = newTrie;
                            currentTrie = newTrie;
                        }
                        else if (existingSegment is VariableValueInsertionTrie existingTrie)
                        {
                            currentTrie = existingTrie;
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }

                    currentTrie[path[^1]] = leaf;
                }
            }
        }
    }

    private static DocumentNode ComposeExecutionDocument(
        OpenApiEndpointDefinition endpoint,
        IDictionary<string, OpenApiModelDefinition> modelsByName)
    {
        List<IExecutableDefinitionNode> definitions =
        [
            .. endpoint.Document.Definitions.OfType<IExecutableDefinitionNode>()
        ];
        var pendingFragments = new Queue<string>(endpoint.ExternalFragmentReferences);
        var processedFragments = new HashSet<string>();

        while (pendingFragments.TryDequeue(out var fragmentName))
        {
            if (!processedFragments.Add(fragmentName)
                || !modelsByName.TryGetValue(fragmentName, out var model))
            {
                continue;
            }

            definitions.AddRange(model.Document.Definitions.OfType<IExecutableDefinitionNode>());

            foreach (var externalFragmentReference in model.ExternalFragmentReferences)
            {
                pendingFragments.Enqueue(externalFragmentReference);
            }
        }

        return new DocumentNode(definitions);
    }

    private static (ITypeDefinition Type, bool HasDefaultValue, bool IsNonNullType) GetParameterDetails(
        OpenApiEndpointDefinitionParameter parameter,
        OperationDefinitionNode operation,
        ISchemaDefinition schema)
    {
        var variable = operation.VariableDefinitions
            .First(v => v.Variable.Name.Value == parameter.VariableName);

        var currentType = schema.Types[variable.Type.NamedType().Name.Value];
        var hasDefaultValue = variable.DefaultValue is not null;
        var isNonNullType = variable.Type.IsNonNullType();

        if (parameter.InputObjectPath is { Length: > 0 })
        {
            foreach (var fieldName in parameter.InputObjectPath)
            {
                if (currentType is not IInputObjectTypeDefinition inputObjectType
                    || !inputObjectType.Fields.TryGetField(fieldName, out var field))
                {
                    throw new InvalidOperationException(
                        $"Expected type '{currentType.Name}' to have a field named '{fieldName}'.");
                }

                currentType = field.Type.NamedType();
                hasDefaultValue = field.DefaultValue is not null;
                isNonNullType = field.Type.IsNonNullType();
            }
        }

        return (currentType, hasDefaultValue, isNonNullType);
    }
}
