using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis.Utilities;

internal sealed class InputCostVisitor : SyntaxWalker<InputCostVisitorContext>
{
    public double CalculateCost(
        IInputValueDefinition argument,
        ArgumentNode argumentNode,
        InputCostVisitorContext context)
    {
        context.Reset();

        context.Types.Push(argument.Type);
        context.Fields.Push(argument);

        Visit(argumentNode.Value, context);
        return context.Cost;
    }

    protected override ISyntaxVisitorAction Enter(
        ObjectFieldNode node,
        InputCostVisitorContext context)
    {
        if (context.Types.TryPeek(out var type)
            && type.NamedType() is InputObjectType inputObject
            && inputObject.Fields.TryGetField(node.Name.Value, out var field))
        {
            context.Cost += field.GetFieldWeight();
            context.Types.Push(field.Type);
            context.Fields.Push(field);
            return Continue;
        }

        return Skip;
    }

    protected override ISyntaxVisitorAction Leave(
        ObjectFieldNode node,
        InputCostVisitorContext context)
    {
        context.Types.Pop();
        context.Fields.Pop();
        return base.Leave(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        VariableNode node,
        InputCostVisitorContext context)
    {
        if (context.Types.TryPeek(out var type))
        {
            var namedInputType = type.NamedType();
            if (namedInputType is InputObjectType inputObjectType)
            {
                context.Cost += ComputeInputObjectCost(inputObjectType, context);
            }

            return Continue;
        }

        return Skip;
    }

    private static double ComputeInputObjectCost(
        InputObjectType type,
        InputCostVisitorContext context)
    {
        if (context.CostCache.TryGetValue(type, out var cached))
        {
            return cached;
        }

        if (!context.Visiting.Add(type))
        {
            // Cycle detected
            context.SubtreeContainsCycle = true;
            return 0;
        }

        var outerSubtreeContainsCycle = context.SubtreeContainsCycle;
        context.SubtreeContainsCycle = false;

        double result;

        if (type.IsOneOf)
        {
            // @oneOf: exactly one field is provided → take the maximum cost of all fields.
            var maxCost = 0.0;
            foreach (var field in type.Fields)
            {
                var fieldCost = field.GetFieldWeight();

                if (field.Type.NamedType() is InputObjectType nestedType)
                {
                    fieldCost += ComputeInputObjectCost(nestedType, context);
                }

                if (fieldCost > maxCost)
                {
                    maxCost = fieldCost;
                }
            }

            result = maxCost;
        }
        else
        {
            // Regular input object: cost is the sum of all fields.
            var totalCost = 0.0;
            foreach (var field in type.Fields)
            {
                totalCost += field.GetFieldWeight();

                if (field.Type.NamedType() is InputObjectType nestedType)
                {
                    totalCost += ComputeInputObjectCost(nestedType, context);
                }
            }

            result = totalCost;
        }

        context.Visiting.Remove(type);

        var currentSubtreeContainsCycle = context.SubtreeContainsCycle;
        if (!currentSubtreeContainsCycle)
        {
            context.CostCache[type] = result;
        }

        // Propagate any cycle that was detected within the current subtree.
        context.SubtreeContainsCycle = outerSubtreeContainsCycle || currentSubtreeContainsCycle;

        return result;
    }
}
