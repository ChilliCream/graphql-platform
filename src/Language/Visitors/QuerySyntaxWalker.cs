using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public class QuerySyntaxWalker
        : SyntaxVisitor<DocumentNode>
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

            VisitSelectionSet(node.SelectionSet);
        }

        protected override void VisitVariableDefinition(
           VariableDefinitionNode node)
        {
            VisitVariable(node.Variable);
            VisitType(node.Type);
            VisitValue(node.DefaultValue);
        }

        protected override void VisitFragmentDefinition(
            FragmentDefinitionNode node)
        {
            VisitName(node.Name);
            VisitMany(node.VariableDefinitions, VisitVariableDefinition);
            VisitNamedType(node.TypeCondition);
            VisitMany(node.Directives, VisitDirective);
            VisitSelectionSet(node.SelectionSet);
        }

        protected virtual void VisitUnsupportedDefinitions(
            IDefinitionNode node)
        {
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
            VisitSelectionSet(node.SelectionSet);
        }

        protected override void VisitListValue(ListValueNode node)
        {
            VisitMany(node.Items, VisitValue);
        }

        protected override void VisitObjectValue(ObjectValueNode node)
        {
            VisitMany(node.Fields, VisitObjectField);
        }

        protected override void VisitObjectField(ObjectFieldNode node)
        {
            VisitName(node.Name);
            VisitValue(node.Value);
        }

        protected override void VisitVariable(VariableNode node)
        {
            VisitName(node.Name);
        }

        protected override void VisitDirective(DirectiveNode node)
        {
            VisitName(node.Name);
            VisitMany(node.Arguments, VisitArgument);
        }

        protected override void VisitArgument(ArgumentNode node)
        {
            VisitName(node.Name);
            VisitValue(node.Value);
        }

        protected override void VisitListType(ListTypeNode node)
        {
            VisitType(node.Type);
        }

        protected override void VisitNonNullType(NonNullTypeNode node)
        {
            VisitType(node.Type);
        }

        protected override void VisitNamedType(NamedTypeNode node)
        {
            VisitName(node.Name);
        }

        protected void VisitMany<T>(IEnumerable<T> items, Action<T> action)
        {
            foreach (T item in items)
            {
                action(item);
            }
        }
    }
}
