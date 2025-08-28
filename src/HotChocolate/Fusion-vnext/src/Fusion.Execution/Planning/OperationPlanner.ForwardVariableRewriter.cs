using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Fusion.Planning;

public sealed partial class OperationPlanner
{
    private static readonly ForwardVariableRewriter s_forwardVariableRewriter = new();

    private sealed class ForwardVariableRewriter : SyntaxRewriter<ForwardVariableRewriter.Context>
    {
        protected override Context OnEnter(ISyntaxNode node, Context context)
        {
            if (node is VariableNode variableNode)
            {
                context.UsedVariables.Add(variableNode.Name.Value);
            }

            return base.OnEnter(node, context);
        }

        protected override OperationDefinitionNode? RewriteOperationDefinition(OperationDefinitionNode node,
            Context context)
        {
            var rewritten = base.RewriteOperationDefinition(node, context);

            if (rewritten is null || context.UsedVariables.Count == 0)
            {
                return rewritten;
            }

            var variableDefinitions = new List<VariableDefinitionNode>();

            foreach (var variableDef in context.Variables)
            {
                if (context.UsedVariables.Remove(variableDef.Key))
                {
                    variableDefinitions.Add(variableDef.Value);
                }
            }

            foreach (var requirement in context.Requirements)
            {
                if (context.UsedVariables.Remove(requirement.Key))
                {
                    variableDefinitions.Add(requirement.Value);
                }
            }

            return rewritten.WithVariableDefinitions(variableDefinitions);
        }

        public sealed class Context
        {
            public OrderedDictionary<string, VariableDefinitionNode> Variables { get; } = [];

            public OrderedDictionary<string, VariableDefinitionNode> Requirements { get; } = [];

            public HashSet<string> UsedVariables { get; } = [];

            public void Reset()
            {
                Requirements.Clear();
                UsedVariables.Clear();
            }
        }
    }
}
