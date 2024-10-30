using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis.Utilities;

internal sealed class InputCostVisitor : SyntaxWalker<InputCostVisitorContext>
{
    public double CalculateCost(IInputField argument, ArgumentNode argumentNode, InputCostVisitorContext context)
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
            var cost = 0.0;

            var namedInputType = type.NamedType();
            if (namedInputType is InputObjectType inputObjectType)
            {
                context.Backlog.Push(inputObjectType);
                while (context.Backlog.TryPop(out var current))
                {
                    foreach (var field in current.Fields)
                    {
                        cost += field.GetFieldWeight();
                        if (field.Type.NamedType() is InputObjectType next && context.Processed.Add(next))
                        {
                            context.Backlog.Push(next);
                        }
                    }
                }
            }

            context.Cost += cost;
            return Continue;
        }

        return Skip;
    }
}
