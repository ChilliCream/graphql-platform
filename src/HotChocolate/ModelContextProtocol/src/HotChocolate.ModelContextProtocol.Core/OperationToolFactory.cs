using System.Collections.Frozen;
using CaseConverter;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.ModelContextProtocol.Extensions;
using HotChocolate.ModelContextProtocol.Storage;
using HotChocolate.Types;
using Json.Schema;
using ModelContextProtocol.Protocol;
using static HotChocolate.ModelContextProtocol.WellKnownFieldNames;

namespace HotChocolate.ModelContextProtocol;

internal sealed class OperationToolFactory(ISchemaDefinition schema)
{
    private static readonly Walker s_walker = new();

    public OperationTool CreateTool(OperationToolDefinition toolDefinition)
    {
        var operationNode = toolDefinition.Document.Definitions.OfType<OperationDefinitionNode>().Single();
        var result = s_walker.Walk(operationNode, toolDefinition.Document, schema);
        var inputSchema = CreateInputSchema(operationNode);
        var outputSchema = CreateOutputSchema(CreateDataSchema(result.Properties, result.RequiredProperties));

        var tool = new Tool
        {
            Name = toolDefinition.Name,
            Title = toolDefinition.Title ?? operationNode.Name!.Value.InsertSpaceBeforeUpperCase(),
            Description = operationNode.Description?.Value,
            InputSchema = inputSchema.ToJsonElement(),
            OutputSchema = outputSchema.ToJsonElement(),
            Annotations = new ToolAnnotations
            {
                DestructiveHint = toolDefinition.DestructiveHint ?? result.DestructiveHint,
                IdempotentHint = toolDefinition.IdempotentHint ?? result.IdempotentHint,
                OpenWorldHint = toolDefinition.OpenWorldHint ?? result.OpenWorldHint,
                ReadOnlyHint = operationNode.Operation is not OperationType.Mutation
            }
        };

        return new OperationTool(toolDefinition.Document, tool);
    }

    private JsonSchema CreateInputSchema(OperationDefinitionNode operation)
    {
        var properties = new Dictionary<string, JsonSchema>();
        var requiredProperties = new List<string>();

        foreach (var variableNode in operation.VariableDefinitions)
        {
            var type = variableNode.Type.ToType(schema);
            var propertyBuilder = type.ToJsonSchemaBuilder();
            var variableName = variableNode.Variable.Name.Value;

            // Description.
            if (variableNode.Description is not null)
            {
                propertyBuilder.Description(variableNode.Description.Value);
            }

            // Default value.
            if (variableNode.DefaultValue is not null)
            {
                propertyBuilder.Default(variableNode.DefaultValue.ToJsonNode(type));
            }

            // Required.
            if (type.IsNonNullType() && variableNode.DefaultValue is null)
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

    private static JsonSchema CreateOutputSchema(JsonSchema dataSchema)
    {
        return
            new JsonSchemaBuilder()
                .Type(SchemaValueType.Object)
                .Properties(
                    (Data, dataSchema),
                    (Errors, s_errorSchema))
                .AdditionalProperties(false)
                .Build();
    }

    private static JsonSchema CreateDataSchema(
        Dictionary<string, JsonSchema> properties,
        List<string> requiredProperties)
    {
        return
            new JsonSchemaBuilder()
                .Type(SchemaValueType.Object | SchemaValueType.Null)
                .Properties(properties)
                .AdditionalProperties(false)
                .Required(requiredProperties);
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
                            Message,
                            new JsonSchemaBuilder().Type(SchemaValueType.String)
                        ),
                        (
                            Locations,
                            new JsonSchemaBuilder()
                                .Type(SchemaValueType.Array | SchemaValueType.Null)
                                .Items(
                                    new JsonSchemaBuilder()
                                        .Type(SchemaValueType.Object)
                                        .Properties(
                                            (Line, s_integerSchema),
                                            (Column, s_integerSchema))
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
                    .Required(Message)
                    .AdditionalProperties(false)
                    .Build())
            .Build();

    private sealed class Walker : SyntaxWalker<WalkerContext>
    {
        public WalkerResult Walk(
            OperationDefinitionNode operationNode,
            DocumentNode documentNode,
            ISchemaDefinition schema)
        {
            var context = new WalkerContext(operationNode.Operation, documentNode, schema);
            context.Frames.Push(new Frame(schema.GetOperationType(operationNode.Operation), [], []));

            Visit(operationNode.SelectionSet, context);

            var rootFrame = context.Frames.Pop();

            return new WalkerResult(
                rootFrame.Properties,
                rootFrame.RequiredProperties,
                context.DestructiveHint,
                context.IdempotentHint,
                context.OpenWorldHint);
        }

        protected override ISyntaxVisitorAction Enter(ISyntaxNode node, WalkerContext context)
        {
            context.Nodes.Push(node);
            return base.Enter(node, context);
        }

        protected override ISyntaxVisitorAction Leave(ISyntaxNode node, WalkerContext context)
        {
            context.Nodes.Pop();
            return base.Leave(node, context);
        }

        protected override ISyntaxVisitorAction Enter(
            FieldNode fieldNode,
            WalkerContext context)
        {
            var type = context.Frames.Peek().Type;

            var field =
                fieldNode.Name.Value == TypeName
                    ? context.Schema.QueryType.Fields[TypeName]
                    : ((IComplexTypeDefinition)type).Fields[fieldNode.Name.Value];

            var parentType =
                context.PendingFields.Count == 0
                    ? null
                    : context.PendingFields.Peek().Field.Type.NamedType();

            var selectionState = fieldNode.GetSelectionState(
                declaringNode: context.Nodes.ElementAtOrDefault(2),
                parentType);

            if (selectionState is SelectionState.Excluded)
            {
                return Skip;
            }

            var pushed = false;

            if (field.Type.NamedType().IsCompositeType())
            {
                context.Frames.Push(new Frame((IOutputTypeDefinition)field.Type.NamedType(), [], []));
                pushed = true;
            }

            var responseName = fieldNode.Alias?.Value ?? fieldNode.Name.Value;
            context.PendingFields.Push(new PendingField(field, responseName, selectionState, pushed));

            var mcpToolAnnotationsDirective = field.GetMcpToolAnnotationsDirective();

            // Only top-level fields.
            if (context.PendingFields.Count == 1)
            {
                // If the destructive hint still has the default value (false).
                if (!context.DestructiveHint)
                {
                    var destructiveHint = mcpToolAnnotationsDirective?.DestructiveHint;

                    // If the tool is explicitly or implicitly destructive.
                    if (destructiveHint is true
                        || (destructiveHint is null && context.OperationType is OperationType.Mutation))
                    {
                        context.DestructiveHint = true;
                    }
                }

                // If the idempotent hint still has the default value (true).
                if (context.IdempotentHint)
                {
                    var idempotentHint = mcpToolAnnotationsDirective?.IdempotentHint;

                    // If the tool is explicitly or implicitly non-idempotent.
                    if (idempotentHint is false
                        || (idempotentHint is null && context.OperationType is OperationType.Mutation))
                    {
                        context.IdempotentHint = false;
                    }
                }
            }

            // If the open world hint still has the default value (false).
            if (!context.OpenWorldHint)
            {
                // If the tool is explicitly or implicitly open world.
                if (mcpToolAnnotationsDirective?.OpenWorldHint is true or null)
                {
                    context.OpenWorldHint = true;
                }
            }

            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            FieldNode fieldNode,
            WalkerContext context)
        {
            if (!context.PendingFields.TryPop(out var pendingField))
            {
                return Continue;
            }

            Frame? childFrame = null;
            if (pendingField.PushedChildFrame)
            {
                childFrame = context.Frames.Pop();
            }

            var (_, properties, requiredProperties) = context.Frames.Peek();

            var propertyJsonSchemaBuilder = pendingField.Field.Type.ToJsonSchemaBuilder();

            if (childFrame is not null)
            {
                if (pendingField.Field.Type.NullableType() is ListType listType)
                {
                    var itemType = SchemaValueType.Object;

                    if (listType.ElementType.IsNullableType())
                    {
                        itemType |= SchemaValueType.Null;
                    }

                    var arrayItemSchemaBuilder =
                        new JsonSchemaBuilder()
                            .Type(itemType)
                            .Properties(childFrame.Value.Properties)
                            .Required(childFrame.Value.RequiredProperties)
                            .AdditionalProperties(false);

                    propertyJsonSchemaBuilder.Items(arrayItemSchemaBuilder);
                }
                else
                {
                    propertyJsonSchemaBuilder.Properties(childFrame.Value.Properties);
                    propertyJsonSchemaBuilder.Required(childFrame.Value.RequiredProperties);
                    propertyJsonSchemaBuilder.AdditionalProperties(false);
                }
            }

            if (pendingField.Field.Description is not null)
            {
                propertyJsonSchemaBuilder.Description(pendingField.Field.Description);
            }

            properties.Add(pendingField.ResponseName, propertyJsonSchemaBuilder.Build());

            if (pendingField.SelectionState is SelectionState.Included)
            {
                requiredProperties.Add(pendingField.ResponseName);
            }

            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            FragmentSpreadNode fragmentSpreadNode,
            WalkerContext context)
        {
            if (context.Fragments.TryGetValue(fragmentSpreadNode.Name.Value, out var fragmentNode))
            {
                Visit(fragmentNode.SelectionSet, context);
            }

            return Skip;
        }

        protected override ISyntaxVisitorAction Enter(
            InlineFragmentNode inlineFragmentNode,
            WalkerContext context)
        {
            // Narrow the type for the duration of this fragment.
            var parent = context.Frames.Peek();
            var narrowed = parent.Type;

            if (inlineFragmentNode.TypeCondition is not null)
            {
                narrowed = (IOutputTypeDefinition)context.Schema.Types[inlineFragmentNode.TypeCondition.Name.Value];
            }

            var shadow = parent with { Type = narrowed };
            context.Frames.Push(shadow);

            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            InlineFragmentNode node,
            WalkerContext context)
        {
            // Pop the shadow frame.
            context.Frames.Pop();
            return Continue;
        }
    }

    private sealed record WalkerResult(
        Dictionary<string, JsonSchema> Properties,
        List<string> RequiredProperties,
        bool DestructiveHint,
        bool IdempotentHint,
        bool OpenWorldHint);

    private sealed class WalkerContext(
        OperationType operationType,
        DocumentNode documentNode,
        ISchemaDefinition schema)
    {
        public OperationType OperationType { get; } = operationType;

        public FrozenDictionary<string, FragmentDefinitionNode> Fragments { get; } =
            documentNode.Definitions.OfType<FragmentDefinitionNode>().ToFrozenDictionary(f => f.Name.Value);

        public ISchemaDefinition Schema { get; } = schema;

        public Stack<ISyntaxNode> Nodes { get; } = [];

        public Stack<Frame> Frames { get; } = [];

        public Stack<PendingField> PendingFields { get; } = [];

        public bool DestructiveHint { get; set; }

        public bool IdempotentHint { get; set; } = true;

        public bool OpenWorldHint { get; set; }
    }

    private readonly record struct Frame(
        IOutputTypeDefinition Type,
        Dictionary<string, JsonSchema> Properties,
        List<string> RequiredProperties);

    private readonly record struct PendingField(
        IOutputFieldDefinition Field,
        string ResponseName,
        SelectionState SelectionState,
        bool PushedChildFrame);
}
