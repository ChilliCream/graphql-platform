using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Delegation
{
    internal class CollectUsedVariableVisitor : QuerySyntaxWalker<ISet<string>>
    {
        public void CollectUsedVariables(
            FieldNode fieldNode,
            ISet<string> usedVariables)
        {
            VisitField(fieldNode, usedVariables);
        }

        public void CollectUsedVariables(
            IEnumerable<FragmentDefinitionNode> fragmentDefinitions,
            ISet<string> usedVariables)
        {
            foreach (FragmentDefinitionNode fragmentDefinition in
                fragmentDefinitions)
            {
                VisitFragmentDefinition(fragmentDefinition, usedVariables);
            }
        }

        protected override void VisitVariable(
            VariableNode node, ISet<string> context)
        {
            context.Add(node.Name.Value);
        }
    }
}
