using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public class QuerySyntaxWalker
        : SyntaxWalkerBase<DocumentNode>
    {
        protected virtual bool VisitFragmentDefinitions => true;

        public override void Visit(DocumentNode node)
        {
            if (node != null)
            {
                VisitDocument(node);
            }
        }

        protected override void VisitDocument(DocumentNode node)
        {
            VisitMany(node.Definitions, VisitDefinition);
        }

        protected virtual void VisitDefinition(IDefinitionNode node)
        {
            switch (node)
            {
                case OperationDefinitionNode value:
                    VisitOperationDefinition(value);
                    break;
                case FragmentDefinitionNode value:
                    if (VisitFragmentDefinitions)
                    {
                        VisitFragmentDefinition(value);
                    }
                    break;
                default:
                    VisitUnsupportedDefinitions(node);
                    break;
            }
        }

        protected override void VisitOperationDefinition(
            OperationDefinitionNode node)
        {
            if (node.Name != null)
            {
                VisitName(node.Name);
                VisitMany(node.VariableDefinitions, VisitVariableDefinition);
                VisitMany(node.Directives, VisitDirective);
            }

            if (node.SelectionSet != null)
            {
                VisitSelectionSet(node.SelectionSet);
            }
        }

        protected override void VisitVariableDefinition(
           VariableDefinitionNode node)
        {
            VisitVariable(node.Variable);
            VisitType(node.Type);


            if (node.DefaultValue != null)
            {
                VisitValue(node.DefaultValue);
            }
        }

        protected override void VisitFragmentDefinition(
            FragmentDefinitionNode node)
        {
            VisitName(node.Name);
            VisitMany(node.VariableDefinitions, VisitVariableDefinition);
            VisitNamedType(node.TypeCondition);
            VisitMany(node.Directives, VisitDirective);

            if (node.SelectionSet != null)
            {
                VisitSelectionSet(node.SelectionSet);
            }
        }

        protected override void VisitSelectionSet(SelectionSetNode node)
        {
            VisitMany(node.Selections, VisitSelection);
        }

        protected override void VisitField(FieldNode node)
        {
            if (node.Alias != null)
            {
                VisitName(node.Alias);
            }

            VisitName(node.Name);
            VisitMany(node.Arguments, VisitArgument);
            VisitMany(node.Directives, VisitDirective);

            if (node.SelectionSet != null)
            {
                VisitSelectionSet(node.SelectionSet);
            }
        }

        protected override void VisitFragmentSpread(FragmentSpreadNode node)
        {
            VisitName(node.Name);
            VisitMany(node.Directives, VisitDirective);
        }

        protected override void VisitInlineFragment(InlineFragmentNode node)
        {
            if (node.TypeCondition != null)
            {
                VisitNamedType(node.TypeCondition);
            }

            VisitMany(node.Directives, VisitDirective);

            if (node.SelectionSet != null)
            {
                VisitSelectionSet(node.SelectionSet);
            }
        }
    }
}
