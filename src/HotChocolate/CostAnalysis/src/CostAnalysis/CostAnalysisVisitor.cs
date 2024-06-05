using System.Diagnostics;
using HotChocolate.CostAnalysis.Directives;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Validation;
using static HotChocolate.CostAnalysis.WellKnownArgumentNames;
using CostDirective = HotChocolate.CostAnalysis.Directives.CostDirective;
using IHasDirectives = HotChocolate.Types.IHasDirectives;

namespace HotChocolate.CostAnalysis;

internal sealed class CostAnalysisVisitor() : TypeDocumentValidatorVisitor(
    new SyntaxVisitorOptions
    {
        VisitArguments = true,
        VisitDirectives = true
    })
{
    private const int DefaultListSize = 1;
    private const string Index = "Index";
    private const string FieldListSize = "FieldListSize";
    private const string ListItemCount = "ListItemCount";
    private const string ListIndex = "ListIndex";

    private readonly CostMetrics _requestCosts = new();
    private readonly List<string> _pathElements = [];
    private readonly NodeContextData _nodeContextData = [];
    private readonly Dictionary<IOutputField, int> _parentFieldListSizes = [];

    private int _fieldCount = 1;
    private int _listSizeProduct = 1;
    private string _path = "";

    protected override ISyntaxVisitorAction Enter(
        OperationDefinitionNode node,
        IDocumentValidatorContext context)
    {
        AddPathElement(context, node);

        Debug.WriteLine($"Entering OperationDefinitionNode: {_path}");

        var action = base.Enter(node, context);

        // Example: Query
        _requestCosts.TypeCounts.Increment(context.Types.Peek().NamedType().Name);

        return action;
    }

    protected override ISyntaxVisitorAction Enter(
        VariableDefinitionNode node,
        IDocumentValidatorContext context)
    {
        AddPathElement(context, node);

        Debug.WriteLine($"Entering VariableDefinitionNode: {_path}");

        _requestCosts.FieldCostByLocation[_path] = 0;

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        DirectiveNode node,
        IDocumentValidatorContext context)
    {
        AddPathElement(context, node);

        Debug.WriteLine($"Entering DirectiveNode: {_path}");

        _requestCosts.FieldCostByLocation[_path] = 0;

        if (context.Schema.TryGetDirectiveType(node.Name.Value, out var directive))
        {
            // Example: @directive
            var schemaCoordinate = new SchemaCoordinate(directive.Name, ofDirective: true);

            _requestCosts.DirectiveCounts.Increment(schemaCoordinate.ToString(), _fieldCount);

            context.Directives.Push(directive);

            return Continue;
        }

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        ArgumentNode node,
        IDocumentValidatorContext context)
    {
        AddPathElement(context, node);

        Debug.WriteLine($"Entering ArgumentNode: {_path}");

        if (context.Directives.TryPeek(out var directive) &&
            directive.Arguments.TryGetField(node.Name.Value, out var directiveArgument))
        {
            // Example: @directive(argument:)
            var schemaCoordinate = new SchemaCoordinate(
                directive.Name,
                argumentName: directiveArgument.Name,
                ofDirective: true);

            _requestCosts.ArgumentCounts.Increment(schemaCoordinate.ToString(), _fieldCount);
            _requestCosts.FieldCostByLocation.Increment(
                _path,
                GetCostWeight(directiveArgument) * _fieldCount);

            if (directiveArgument.Type.NamedType() is InputObjectType inputType)
            {
                _requestCosts.InputTypeCounts.Increment(inputType.Name, _fieldCount);
            }

            context.InputFields.Push(directiveArgument);
            context.Types.Push(directiveArgument.Type);

            return Continue;
        }

        if (context.OutputFields.TryPeek(out var field) &&
            field.Arguments.TryGetField(node.Name.Value, out var fieldArgument))
        {
            // Example: Type.field(argument:)
            var schemaCoordinate = new SchemaCoordinate(
                field.DeclaringType.Name,
                field.Name,
                fieldArgument.Name);

            _requestCosts.ArgumentCounts.Increment(schemaCoordinate.ToString(), _fieldCount);
            _requestCosts.FieldCostByLocation.Increment(
                _path,
                GetCostWeight(fieldArgument) * _fieldCount);

            if (fieldArgument.Type.NamedType() is InputObjectType inputType)
            {
                _requestCosts.InputTypeCounts.Increment(inputType.Name, _fieldCount);
            }

            context.InputFields.Push(fieldArgument);
            context.Types.Push(fieldArgument.Type);

            return Continue;
        }

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        ObjectFieldNode node,
        IDocumentValidatorContext context)
    {
        AddPathElement(context, node);

        Debug.WriteLine($"Entering ObjectFieldNode: {_path}");

        if (context.Types.TryPeek(out var type) &&
            type.NamedType() is InputObjectType inputType &&
            inputType.Fields.TryGetField(node.Name.Value, out var inputField))
        {
            _requestCosts.InputFieldCounts.Increment(inputField.Coordinate.ToString(), _fieldCount);
            _requestCosts.FieldCostByLocation.Increment(
                _path,
                GetCostWeight(inputField) * _fieldCount);

            if (inputField.Type.NamedType() is InputObjectType inputFieldType)
            {
                _requestCosts.InputTypeCounts.Increment(inputFieldType.Name, _fieldCount);
            }

            context.InputFields.Push(inputField);
            context.Types.Push(inputField.Type);

            return Continue;
        }

        return Skip;
    }

    protected override ISyntaxVisitorAction Leave(
        ObjectFieldNode node,
        IDocumentValidatorContext context)
    {
        Debug.WriteLine($"Leaving ObjectFieldNode: {_path}");

        var childCost = _requestCosts.FieldCostByLocation[_path];
        RemovePathElement();
        _requestCosts.FieldCostByLocation.Increment(_path, childCost);

        context.InputFields.Pop();
        context.Types.Pop();

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        ArgumentNode node,
        IDocumentValidatorContext context)
    {
        Debug.WriteLine($"Leaving ArgumentNode: {_path}");

        var childCost = _requestCosts.FieldCostByLocation[_path];
        RemovePathElement();
        _requestCosts.FieldCostByLocation.Increment(_path, childCost);

        context.InputFields.Pop();
        context.Types.Pop();

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        DirectiveNode node,
        IDocumentValidatorContext context)
    {
        Debug.WriteLine($"Leaving DirectiveNode: {_path}");

        var childCost = _requestCosts.FieldCostByLocation[_path];
        RemovePathElement();
        _requestCosts.FieldCostByLocation.Increment(_path, childCost);

        context.Directives.Pop();

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        VariableDefinitionNode node,
        IDocumentValidatorContext context)
    {
        Debug.WriteLine($"Leaving VariableDefinitionNode: {_path}");

        var childCost = _requestCosts.FieldCostByLocation[_path];
        RemovePathElement();
        _requestCosts.FieldCostByLocation.Increment(_path, childCost);

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        SelectionSetNode node,
        IDocumentValidatorContext context)
    {
        Debug.WriteLine($"Entering SelectionSetNode: {_path}");

        var uniqueNodeCounts = new Dictionary<string, int>();

        // Calculate counts per unique node (by name).
        foreach (var selection in node.Selections)
        {
            if (selection is FieldNode or InlineFragmentNode)
            {
                uniqueNodeCounts.Increment(GetNodeName(selection));
            }
        }

        // Remove counts <= 1.
        var uniqueNodeCountsAboveOne = new Dictionary<string, int>();

        foreach (var (name, count) in uniqueNodeCounts)
        {
            if (count > 1)
            {
                uniqueNodeCountsAboveOne.Add(name, count);
            }
        }

        // Assign indexes to nodes for later use.
        for (var i = node.Selections.Count - 1; i >= 0; i--)
        {
            var selection = node.Selections[i];

            if (selection is FieldNode or InlineFragmentNode)
            {
                var nodeName = GetNodeName(selection);

                if (uniqueNodeCountsAboveOne.TryGetValue(nodeName, out var count))
                {
                    _nodeContextData.Set(selection, Index, count - 1);

                    uniqueNodeCountsAboveOne[nodeName] = count - 1;
                }
            }
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        IDocumentValidatorContext context)
    {
        if (context.Types.TryPeek(out var type) &&
            type.NamedType() is IComplexOutputType outputType &&
            outputType.Fields.TryGetField(node.Name.Value, out var outputField))
        {
            // Skip introspection fields.
            if (outputField.IsIntrospectionField)
            {
                return Skip;
            }

            AddPathElement(context, node);
            Debug.WriteLine($"Entering FieldNode: {_path}");

            // Capture the field count to be used for other counts within the field.
            _fieldCount = _listSizeProduct;

            // Example: Type.field
            _requestCosts.FieldCounts.Increment(outputField.Coordinate.ToString(), _fieldCount);
            _requestCosts.FieldCostByLocation.Increment(
                _path,
                GetCostWeight(outputField) * _fieldCount);

            var listSizeDirective = outputField.Directives.FirstOrDefault<ListSizeDirective>();

            if (listSizeDirective is null)
            {
                // Check parent field for listSize directive.
                if (context.OutputFields.TryPeek(out var parentField))
                {
                    listSizeDirective = parentField.Directives.FirstOrDefault<ListSizeDirective>();

                    if (listSizeDirective is not null)
                    {
                        var sizedFields =
                            listSizeDirective.GetArgumentValue<List<string>?>(SizedFields);

                        if (sizedFields?.Contains(outputField.Name) == true)
                        {
                            // Take list size from parent field.
                            _nodeContextData.Set(
                                node,
                                FieldListSize,
                                _parentFieldListSizes[parentField]);

                            _listSizeProduct *= _parentFieldListSizes[parentField];
                        }
                    }
                }
            }
            else
            {
                var sizedFields = listSizeDirective.GetArgumentValue<List<string>?>(SizedFields);
                var listSize = GetListSize(context, listSizeDirective, node, outputField);

                if (sizedFields is null)
                {
                    // Apply the list size to the current field.
                    _nodeContextData.Set(node, FieldListSize, listSize);

                    _listSizeProduct *= listSize;
                }
                else
                {
                    // Store the list size to be accessed via the parent field.
                    _parentFieldListSizes.Add(outputField, listSize);
                }
            }

            // Example: Type
            _requestCosts.TypeCounts.Increment(outputField.Type.NamedType().Name, _listSizeProduct);
            _requestCosts.TypeCostByLocation.Increment(
                _path,
                GetCostWeight(outputField.Type.NamedType()) * _listSizeProduct);

            context.OutputFields.Push(outputField);
            context.Types.Push(outputField.Type);

            return Continue;
        }

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        ListValueNode node,
        IDocumentValidatorContext context)
    {
        _nodeContextData.Set(node, ListItemCount, node.Items.Count);
        _nodeContextData.Set(node, ListIndex, 0);

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        ObjectValueNode node,
        IDocumentValidatorContext context)
    {
        if (context.Path.Peek() is ListValueNode listValueNode)
        {
            var itemCount = _nodeContextData.Get(listValueNode, ListItemCount);
            var currentIndex = _nodeContextData.Get(listValueNode, ListIndex);

            if (itemCount > 1)
            {
                AddPathElement($"[{currentIndex}]");
                _nodeContextData.Set(listValueNode, ListIndex, ++currentIndex);
            }
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        ObjectValueNode node,
        IDocumentValidatorContext context)
    {
        if (context.Path.Peek() is ListValueNode)
        {
            var childCost = _requestCosts.FieldCostByLocation[_path];
            RemovePathElement();
            _requestCosts.FieldCostByLocation.Increment(_path, childCost);
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        ListValueNode node,
        IDocumentValidatorContext context)
    {
        _nodeContextData.Remove(node, ListItemCount);

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        InlineFragmentNode node,
        IDocumentValidatorContext context)
    {
        AddPathElement(context, node);

        Debug.WriteLine($"Entering InlineFragmentNode: {_path}");

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Leave(
        InlineFragmentNode node,
        IDocumentValidatorContext context)
    {
        Debug.WriteLine($"Leaving InlineFragmentNode: {_path}");

        var childCost = _requestCosts.FieldCostByLocation[_path];
        RemovePathElement();
        _requestCosts.FieldCostByLocation.Increment(_path, childCost);

        return base.Leave(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        FragmentSpreadNode node,
        IDocumentValidatorContext context)
    {
        AddPathElement(context, node);

        Debug.WriteLine($"Entering FragmentSpreadNode: {_path}");

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        FragmentSpreadNode node,
        IDocumentValidatorContext context)
    {
        Debug.WriteLine($"Leaving FragmentSpreadNode: {_path}");

        var childCost = _requestCosts.FieldCostByLocation[_path];
        RemovePathElement();
        _requestCosts.FieldCostByLocation.Increment(_path, childCost);

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        FragmentDefinitionNode node,
        IDocumentValidatorContext context)
    {
        // FIXME: Fragment definitions (~~fragment) should start from the root.
        AddPathElement(context, node);

        Debug.WriteLine($"Entering FragmentDefinitionNode: {_path}");

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Leave(
        FragmentDefinitionNode node,
        IDocumentValidatorContext context)
    {
        Debug.WriteLine($"Leaving FragmentDefinitionNode: {_path}");

        var childCost = _requestCosts.FieldCostByLocation[_path];
        RemovePathElement();
        _requestCosts.FieldCostByLocation.Increment(_path, childCost);

        return base.Leave(node, context);
    }

    protected override ISyntaxVisitorAction Leave(
        FieldNode node,
        IDocumentValidatorContext context)
    {
        Debug.WriteLine($"Leaving FieldNode: {_path}");

        // https://ibm.github.io/graphql-specs/cost-spec.html#sec-weight
        // "The overall calculated weight of a field or directive can never be negative, and if
        // found to be negative must be rounded up to zero"
        var childFieldCost = Math.Max(_requestCosts.FieldCostByLocation[_path], 0);
        var childTypeCost = Math.Max(_requestCosts.TypeCostByLocation[_path], 0);
        RemovePathElement();
        _requestCosts.FieldCostByLocation.Increment(_path, childFieldCost);
        _requestCosts.TypeCostByLocation.Increment(_path, childTypeCost);

        context.Types.Pop();
        var outputField = context.OutputFields.Pop();

        if (_nodeContextData.TryGet(node, FieldListSize, out var listSize))
        {
            _listSizeProduct /= listSize;
        }

        _nodeContextData.Remove(node, FieldListSize);
        _parentFieldListSizes.Remove(outputField);

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        OperationDefinitionNode node,
        IDocumentValidatorContext context)
    {
        Debug.WriteLine($"Leaving OperationDefinitionNode: {_path}");

        _requestCosts.FieldCost = _requestCosts.FieldCostByLocation[_path];
        _requestCosts.TypeCost = _requestCosts.TypeCostByLocation[_path];

        context.ContextData.Add(WellKnownContextData.RequestCosts, _requestCosts);

        return base.Leave(node, context);
    }

    private void AddPathElement(IDocumentValidatorContext context, ISyntaxNode node)
    {
        var pathElement = GetPathSegment(context, node);
        _pathElements.Push(pathElement);
        _path += pathElement;
    }

    private void AddPathElement(string pathElement)
    {
        _pathElements.Push(pathElement);
        _path += pathElement;
    }

    private void RemovePathElement()
    {
        var pathElement = _pathElements.Pop();
        _path = _path.Remove(_path.Length - pathElement.Length);
    }

    private static double GetCostWeight(IInputField field)
    {
        // Use weight from @cost directive.
        if (field is IHasDirectives fieldWithDirectives)
        {
            var costDirective =
                fieldWithDirectives.Directives
                    .FirstOrDefault<CostDirective>()?.AsValue<CostDirective>();

            if (costDirective is not null)
            {
                return double.Parse(costDirective.Weight);
            }
        }

        // https://ibm.github.io/graphql-specs/cost-spec.html#sec-weight
        // "Weights for all composite input and output types default to "1.0""
        return field.Type.IsCompositeType() || field.Type.IsListType() ? 1.0 : 0.0;
    }

    private static double GetCostWeight(IOutputField field)
    {
        // Use weight from @cost directive.
        if (field is IHasDirectives fieldWithDirectives)
        {
            var costDirective =
                fieldWithDirectives.Directives
                    .FirstOrDefault<CostDirective>()?.AsValue<CostDirective>();

            if (costDirective is not null)
            {
                return double.Parse(costDirective.Weight);
            }
        }

        // https://ibm.github.io/graphql-specs/cost-spec.html#sec-weight
        // "Weights for all composite input and output types default to "1.0""
        return field.Type.IsCompositeType() || field.Type.IsListType() ? 1.0 : 0.0;
    }

    private static double GetCostWeight(IType type)
    {
        // Use weight from @cost directive.
        if (type is IHasDirectives typeWithDirectives)
        {
            var costDirective =
                typeWithDirectives.Directives
                    .FirstOrDefault<CostDirective>()?.AsValue<CostDirective>();

            if (costDirective is not null)
            {
                return double.Parse(costDirective.Weight);
            }
        }

        // https://ibm.github.io/graphql-specs/cost-spec.html#sec-weight
        // "Weights for all composite input and output types default to "1.0""
        return type.IsCompositeType() || type.IsListType() ? 1.0 : 0.0;
    }

    private static int GetListSize(
        IDocumentValidatorContext context,
        Directive listSizeDirective,
        FieldNode fieldNode,
        IOutputField outputField)
    {
        var slicingArgumentNames =
            listSizeDirective.GetArgumentValue<List<string>?>(SlicingArguments);

        if (slicingArgumentNames is not null)
        {
            var requireOneSlicingArgument =
                listSizeDirective.GetArgumentValue<bool>(RequireOneSlicingArgument);

            List<int> slicingValues = [];
            var argumentCount = 0;

            foreach (var slicingArgumentName in slicingArgumentNames)
            {
                var slicingArgument =
                    fieldNode.Arguments.SingleOrDefault(a => a.Name.Value == slicingArgumentName);

                if (slicingArgument is not null)
                {
                    argumentCount++;

                    switch (slicingArgument.Value)
                    {
                        case IntValueNode intValueNode:
                            slicingValues.Add(intValueNode.ToInt32());

                            continue;

                        case VariableNode variableNode
                            when context.Variables[variableNode.Name.Value].DefaultValue is
                                IntValueNode intValueNode:

                            slicingValues.Add(intValueNode.ToInt32());

                            continue;
                    }
                }

                var defaultValue = outputField.Arguments[slicingArgumentName].DefaultValue;

                if (defaultValue is IntValueNode defaultValueNode)
                {
                    slicingValues.Add(defaultValueNode.ToInt32());
                }
            }

            // The `requireOneSlicingArgument` argument can be used to inform the static analysis
            // that it should expect that exactly one of the defined slicing arguments is present in
            // a query. If that is not the case (i.e., if none or multiple slicing arguments are
            // present), the static analysis may throw an error.
            if (requireOneSlicingArgument && argumentCount != 1)
            {
                // FIXME: Exception type, handling, and localization.
                throw new Exception(
                    $"Expected 1 slicing argument, {argumentCount} provided.");
            }

            if (slicingValues.Count != 0)
            {
                return slicingValues.Max();
            }
        }

        var assumedSize = listSizeDirective.GetArgumentValue<int?>(AssumedSize);

        return assumedSize ?? DefaultListSize;
    }

    private static string GetNodeName(ISelectionNode selection)
    {
        return selection switch
        {
            FieldNode f => f.Alias?.Value ?? f.Name.Value,
            InlineFragmentNode i => i.TypeCondition?.Name() ?? "~",
            _ => throw new ArgumentOutOfRangeException(nameof(selection), selection, null)
        };
    }

    private string GetPathSegment(IDocumentValidatorContext context, ISyntaxNode node)
    {
        return node switch
        {
            ArgumentNode a => $"({a.Name.Value}:)",
            DirectiveNode d => $".@{d.Name.Value}",
            FieldNode f => $".{f.Alias?.Value ?? f.Name.Value}{GetIndexSuffix(f)}",
            FragmentDefinitionNode f => $"~~{f.Name.Value}",
            FragmentSpreadNode f => $".~{f.Name.Value}",
            InlineFragmentNode i => $".on~{GetFragmentTypeName(i, context)}{GetIndexSuffix(i)}",
            ObjectFieldNode o => $".{o.Name.Value}",
            OperationDefinitionNode o => o.Name?.Value ?? o.Operation.ToString().ToLowerInvariant(),
            VariableDefinitionNode v => $"(${v.Variable.Name.Value}:)",
            _ => throw new NotImplementedException()
        };
    }

    private string? GetIndexSuffix(ISyntaxNode node)
    {
        if (_nodeContextData.TryGetValue(node, out var contextData) &&
            contextData.TryGetValue(Index, out var index))
        {
            return $"[{index}]";
        }

        return null;
    }

    private static string GetFragmentTypeName(
        InlineFragmentNode fragmentNode,
        IDocumentValidatorContext context)
    {
        return
            fragmentNode.TypeCondition?.Name.Value ?? context.OutputFields.Peek().Type.TypeName();
    }
}

file static class DictionaryExtensions
{
    public static void Increment(
        this Dictionary<string, double> dictionary,
        string key,
        double amount = 1)
    {
        if (dictionary.TryGetValue(key, out var count))
        {
            dictionary[key] = count + amount;
        }
        else
        {
            dictionary[key] = amount;
        }
    }

    public static void Increment(
        this Dictionary<string, int> dictionary,
        string key,
        int amount = 1)
    {
        if (dictionary.TryGetValue(key, out var count))
        {
            dictionary[key] = count + amount;
        }
        else
        {
            dictionary[key] = amount;
        }
    }
}
