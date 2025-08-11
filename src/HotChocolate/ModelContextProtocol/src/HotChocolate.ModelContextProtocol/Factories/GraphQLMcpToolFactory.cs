using CaseConverter;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.ModelContextProtocol.Extensions;
using HotChocolate.Types;
using Json.Schema;
using ModelContextProtocol.Protocol;
using static HotChocolate.ModelContextProtocol.WellKnownArgumentNames;
using static HotChocolate.ModelContextProtocol.WellKnownDirectiveNames;

namespace HotChocolate.ModelContextProtocol.Factories;

internal sealed class GraphQLMcpToolFactory(ISchemaDefinition graphQLSchema)
{
    public GraphQLMcpTool CreateTool(string name, DocumentNode document)
    {
        var operationNode = document.Definitions.OfType<OperationDefinitionNode>().Single();
        var mcpToolDirective = operationNode.GetMcpToolDirective();
        var operationCompiler = new OperationCompiler(new InputParser());
        var operation =
            operationCompiler.Compile(
                new OperationCompilerRequest(
                    operationNode.Name!.Value,
                    document,
                    operationNode,
                    (ObjectType)graphQLSchema.GetOperationType(operationNode.Operation),
                    graphQLSchema));
        var inputSchema = CreateInputSchema(operationNode);
        var outputSchema = CreateOutputSchema(operation);

        var tool = new Tool
        {
            Name = name,
            Title = mcpToolDirective?.Title ?? operation.Name!.InsertSpaceBeforeUpperCase(),
            Description = operationNode.Description?.Value,
            InputSchema = inputSchema.ToJsonElement(),
            OutputSchema = outputSchema.ToJsonElement(),
            Annotations = new ToolAnnotations
            {
                DestructiveHint = GetDestructiveHint(operation),
                IdempotentHint = GetIdempotentHint(operation),
                OpenWorldHint = GetOpenWorldHint(operation),
                ReadOnlyHint = operationNode.Operation is not OperationType.Mutation
            }
        };

        return new GraphQLMcpTool(operation, tool);
    }

    private JsonSchema CreateInputSchema(OperationDefinitionNode operation)
    {
        var properties = new Dictionary<string, JsonSchema>();
        var requiredProperties = new List<string>();

        foreach (var variableNode in operation.VariableDefinitions)
        {
            var graphQLType = variableNode.Type.GetGraphQLType(graphQLSchema);
            var propertyBuilder = graphQLType.ToJsonSchemaBuilder();
            var variableName = variableNode.Variable.Name.Value;

            // Description.
            if (variableNode.Description is not null)
            {
                propertyBuilder.Description(variableNode.Description.Value);
            }

            // Default value.
            if (variableNode.DefaultValue is not null)
            {
                propertyBuilder.Default(variableNode.DefaultValue.ToJsonNode(graphQLType));
            }

            // Required.
            if (graphQLType.IsNonNullType() && variableNode.DefaultValue is null)
            {
                requiredProperties.Add(variableName);
            }

            properties.Add(variableName, propertyBuilder);
        }

        return
            new JsonSchemaBuilder()
                .Type(SchemaValueType.Object)
                .Properties(properties)
                .Required(requiredProperties)
                .Build();
    }

    private static JsonSchema CreateOutputSchema(IOperation operation)
    {
        return
            new JsonSchemaBuilder()
                .Type(SchemaValueType.Object)
                .Properties(
                    (WellKnownFieldNames.Data, CreateDataSchema(operation)),
                    (WellKnownFieldNames.Errors, s_errorSchema))
                .AdditionalProperties(false)
                .Build();
    }

    private static JsonSchema CreateDataSchema(IOperation operation)
    {
        var properties = new Dictionary<string, JsonSchema>();
        var requiredProperties = new List<string>();

        foreach (var rootSelection in operation.RootSelectionSet.Selections)
        {
            var selectionState = rootSelection.GetSelectionState();

            if (selectionState is SelectionState.Excluded)
            {
                continue;
            }

            properties.Add(
                rootSelection.ResponseName,
                CreateOutputSchema(rootSelection, operation));

            if (selectionState is SelectionState.Included)
            {
                requiredProperties.Add(rootSelection.ResponseName);
            }
        }

        return
            new JsonSchemaBuilder()
                .Type(SchemaValueType.Object | SchemaValueType.Null)
                .Properties(properties)
                .AdditionalProperties(false)
                .Required(requiredProperties)
                .Build();
    }

    private static JsonSchema CreateOutputSchema(ISelection selection, IOperation operation)
    {
        var schemaBuilder = selection.Field.Type.ToJsonSchemaBuilder();

        if (selection.SelectionSet is not null)
        {
            var properties = new Dictionary<string, JsonSchema>();
            var requiredProperties = new List<string>();

            foreach (var type in operation.GetPossibleTypes(selection))
            {
                var selectionSet = operation.GetSelectionSet(selection, type);

                foreach (var subSelection in selectionSet.Selections)
                {
                    var selectionState = subSelection.GetSelectionState();

                    if (selectionState is SelectionState.Excluded)
                    {
                        continue;
                    }

                    var propertyAdded =
                        properties.TryAdd(
                            subSelection.ResponseName,
                            CreateOutputSchema(subSelection, operation));

                    if (propertyAdded && selectionState is SelectionState.Included)
                    {
                        requiredProperties.Add(subSelection.ResponseName);
                    }
                }
            }

            if (selection.Field.Type.NullableType() is ListType listType)
            {
                var itemType = SchemaValueType.Object;

                if (listType.ElementType.IsNullableType())
                {
                    itemType |= SchemaValueType.Null;
                }

                var arrayItemSchemaBuilder
                    = new JsonSchemaBuilder()
                        .Type(itemType)
                        .Properties(properties)
                        .Required(requiredProperties)
                        .AdditionalProperties(false);

                schemaBuilder.Items(arrayItemSchemaBuilder);
            }
            else
            {
                schemaBuilder
                    .Properties(properties)
                    .Required(requiredProperties)
                    .AdditionalProperties(false);
            }
        }

        // Description.
        if (selection.Field.Description is not null)
        {
            schemaBuilder.Description(selection.Field.Description);
        }

        return schemaBuilder.Build();
    }

    private static bool GetDestructiveHint(IOperation operation)
    {
        // @mcpTool operation directive.
        if (operation.Definition.TryGetMcpToolDirective(out var mcpToolDirective)
            && mcpToolDirective.DestructiveHint is { } destructiveHint)
        {
            return destructiveHint;
        }

        // @mcpToolAnnotations field directive.
        var destructiveHints =
            operation.RootSelectionSet.Selections
                .Select(
                    s => s
                        .Field.Directives[McpToolAnnotations]
                        .SingleOrDefault()?
                        .GetArgumentValue<bool?>(DestructiveHint)
                            // Default to `true` for mutations.
                            ?? operation.Type is OperationType.Mutation)
                .ToList();

        // Return `true` if any of the destructive hints are `true`.
        return destructiveHints.Any(d => d);
    }

    private static bool GetIdempotentHint(IOperation operation)
    {
        // @mcpTool operation directive.
        if (operation.Definition.TryGetMcpToolDirective(out var mcpToolDirective)
            && mcpToolDirective.IdempotentHint is { } idempotentHint)
        {
            return idempotentHint;
        }

        // @mcpToolAnnotations field directive.
        var idempotentHints =
            operation.RootSelectionSet.Selections
                .Select(
                    s => s
                        .Field.Directives[McpToolAnnotations]
                        .SingleOrDefault()?
                        .GetArgumentValue<bool?>(IdempotentHint)
                            // Default to `true` for queries and subscriptions.
                            ?? operation.Type is not OperationType.Mutation)
                .ToList();

        // Return `true` if all the idempotent hints are `true`.
        return idempotentHints.All(i => i);
    }

    private static bool GetOpenWorldHint(IOperation operation)
    {
        // @mcpTool operation directive.
        if (operation.Definition.TryGetMcpToolDirective(out var mcpToolDirective)
            && mcpToolDirective.OpenWorldHint is { } openWorldHint)
        {
            return openWorldHint;
        }

        // @mcpToolAnnotations field directive.
        List<bool> openWorldHints = [];
        foreach (var rootSelection in operation.RootSelectionSet.Selections)
        {
            var rootOpenWorldHint = GetOpenWorldHint(rootSelection, operation);

            // Default to `true`.
            openWorldHints.Add(rootOpenWorldHint ?? true);
        }

        // Return `true` if any of the open world hints are `true`.
        return openWorldHints.Any(i => i);
    }

    private static bool? GetOpenWorldHint(
        ISelection selection,
        IOperation operation,
        bool? parentOpenWorldHint = null)
    {
        var openWorldHint =
            selection.Field.Directives[McpToolAnnotations]
                .SingleOrDefault()?
                .GetArgumentValue<bool?>(OpenWorldHint) ?? parentOpenWorldHint;

        // Return early if the open world hint is explicitly set to `true`.
        if (openWorldHint == true)
        {
            return openWorldHint;
        }

        List<bool?> openWorldHints = [openWorldHint];

        if (selection.SelectionSet is not null)
        {
            foreach (var type in operation.GetPossibleTypes(selection))
            {
                var selectionSet = operation.GetSelectionSet(selection, type);

                foreach (var subSelection in selectionSet.Selections)
                {
                    openWorldHints.Add(
                        GetOpenWorldHint(
                            subSelection,
                            operation,
                            parentOpenWorldHint: openWorldHint));
                }
            }
        }

        return openWorldHints.All(o => o is null)
            ? null
            : openWorldHints.Any(o => o == true);
    }

    private static readonly JsonSchema s_integerSchema =
        new JsonSchemaBuilder()
            .Type(SchemaValueType.Integer)
            .Build();

    private static readonly JsonSchema s_errorSchema =
        new JsonSchemaBuilder()
            .Type(SchemaValueType.Array)
            .Items(
                new JsonSchemaBuilder()
                    .Type(SchemaValueType.Object)
                    .Properties(
                        (
                            WellKnownFieldNames.Message,
                            new JsonSchemaBuilder().Type(SchemaValueType.String)
                        ),
                        (
                            WellKnownFieldNames.Locations,
                            new JsonSchemaBuilder()
                                .Type(SchemaValueType.Array | SchemaValueType.Null)
                                .Items(
                                    new JsonSchemaBuilder()
                                        .Type(SchemaValueType.Object)
                                        .Properties(
                                            (WellKnownFieldNames.Line, s_integerSchema),
                                            (WellKnownFieldNames.Column, s_integerSchema))
                                        .AdditionalProperties(false))
                        ),
                        (
                            WellKnownFieldNames.Path,
                            new JsonSchemaBuilder()
                                .Type(SchemaValueType.Array | SchemaValueType.Null)
                                .Items(
                                    new JsonSchemaBuilder()
                                        .Type(SchemaValueType.String | SchemaValueType.Integer))
                        ),
                        (
                            WellKnownFieldNames.Extensions,
                            new JsonSchemaBuilder()
                                .Type(SchemaValueType.Object | SchemaValueType.Null)
                                .AdditionalProperties(true)
                        ))
                    .Required(WellKnownFieldNames.Message)
                    .AdditionalProperties(false)
                    .Build())
            .Build();
}
