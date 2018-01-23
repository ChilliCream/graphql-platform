using System.Collections.Generic;
using GraphQLParser.AST;

namespace Zeus
{
    public class SyntaxNodeWalker
        : SyntaxNodeVisitor
    {
        protected SyntaxNodeWalker()
        {
        }

        protected virtual void Visit(IEnumerable<ASTNode> nodes)
        {
            if (nodes != null)
            {
                foreach (ASTNode node in nodes)
                {
                    Visit(node);
                }
            }
        }

        protected override void VisitArgument(GraphQLArgument argument)
        {
            Visit(argument.Name);
            Visit(argument.Value);
        }

        protected override void VisitDirective(GraphQLDirective directive)
        {
            Visit(directive.Name);
            Visit(directive.Arguments);
        }

        protected override void VisitDocument(GraphQLDocument document)
        {
            Visit(document.Definitions);
        }

        protected override void VisitOperationDefinition(GraphQLOperationDefinition operationDefinition)
        {
            Visit(operationDefinition.Directives);
            Visit(operationDefinition.Name);
            Visit(operationDefinition.VariableDefinitions);
            Visit(operationDefinition.SelectionSet);
        }

        protected override void VisitVariableDefinition(GraphQLVariableDefinition variableDefinition)
        {
            Visit(variableDefinition.Type);
            Visit(variableDefinition.Variable);
        }

        protected override void VisitVariable(GraphQLVariable variable)
        {
            Visit(variable.Name);
        }

        protected override void VisitSelectionSet(GraphQLSelectionSet selectionSet)
        {
            Visit(selectionSet.Selections);
        }

        protected override void VisitField(GraphQLFieldSelection field)
        {
            Visit(field.Directives);
            Visit(field.Name);
            Visit(field.Alias);
            Visit(field.Arguments);
            Visit(field.SelectionSet);
        }

        protected override void VisitFragmentSpread(GraphQLFragmentSpread fragmentSpread)
        {
            Visit(fragmentSpread.Directives);
            Visit(fragmentSpread.Name);
        }

        protected override void VisitInlineFragment(GraphQLInlineFragment inlineFragment)
        {
            Visit(inlineFragment.Directives);
            Visit(inlineFragment.TypeCondition);
            Visit(inlineFragment.SelectionSet);
        }

        protected override void VisitFragmentDefinition(GraphQLFragmentDefinition fragmentDefinition)
        {
            Visit(fragmentDefinition.Directives);
            Visit(fragmentDefinition.Name);
            Visit(fragmentDefinition.TypeCondition);
            Visit(fragmentDefinition.SelectionSet);
        }

        protected override void VisitListValue(GraphQLListValue listValue)
        {
            Visit(listValue.Values);
        }

        protected override void VisitObjectValue(GraphQLObjectValue objectValue)
        {
            Visit(objectValue.Fields);
        }

        protected override void VisitObjectField(GraphQLObjectField objectField)
        {
            Visit(objectField.Name);
            Visit(objectField.Value);
        }

        protected override void VisitNamedType(GraphQLNamedType namedType)
        {
            Visit(namedType.Name);
        }

        protected override void VisitListType(GraphQLListType listType)
        {
            Visit(listType.Type);
        }

        protected override void VisitNonNullType(GraphQLNonNullType nonNullType)
        {
            Visit(nonNullType.Type);
        }

        protected override void VisitSchemaDefinition(GraphQLSchemaDefinition schemaDefinition)
        {
            Visit(schemaDefinition.Directives);
            Visit(schemaDefinition.OperationTypes);
        }

        protected override void VisitOperationTypeDefinition(GraphQLOperationTypeDefinition operationTypeDefinition)
        {
            Visit(operationTypeDefinition.Type);
        }

        protected override void VisitScalarTypeDefinition(GraphQLScalarTypeDefinition scalarTypeDefinition)
        {
            Visit(scalarTypeDefinition.Directives);
            Visit(scalarTypeDefinition.Name);

        }

        protected override void VisitObjectTypeDefinition(GraphQLObjectTypeDefinition objectTypeDefinition)
        {
            Visit(objectTypeDefinition.Directives);
            Visit(objectTypeDefinition.Name);
            Visit(objectTypeDefinition.Interfaces);
            Visit(objectTypeDefinition.Fields);
        }

        protected override void VisitFieldDefinition(GraphQLFieldDefinition fieldDefinition)
        {
            Visit(fieldDefinition.Directives);
            Visit(fieldDefinition.Name);
            Visit(fieldDefinition.Type);
            Visit(fieldDefinition.Arguments);
        }

        protected override void VisitInputValueDefinition(GraphQLInputValueDefinition inputValueDefinition)
        {
            Visit(inputValueDefinition.Directives);
            Visit(inputValueDefinition.Name);
            Visit(inputValueDefinition.Type);
            Visit(inputValueDefinition.DefaultValue);
        }

        protected override void VisitInterfaceTypeDefinition(GraphQLInterfaceTypeDefinition interfaceTypeDefinition)
        {
            Visit(interfaceTypeDefinition.Directives);
            Visit(interfaceTypeDefinition.Name);
            Visit(interfaceTypeDefinition.Fields);
        }

        protected override void VisitUnionTypeDefinition(GraphQLUnionTypeDefinition unionTypeDefinition)
        {
            Visit(unionTypeDefinition.Directives);
            Visit(unionTypeDefinition.Types);
        }

        protected override void VisitEnumTypeDefinition(GraphQLEnumTypeDefinition enumTypeDefinition)
        {
            Visit(enumTypeDefinition.Directives);
            Visit(enumTypeDefinition.Name);
            Visit(enumTypeDefinition.Values);
        }

        protected override void VisitEnumValueDefinition(GraphQLEnumValueDefinition enumValueDefinition)
        {
            Visit(enumValueDefinition.Directives);
            Visit(enumValueDefinition.Name);
        }

        protected override void VisitInputObjectTypeDefinition(GraphQLInputObjectTypeDefinition inputObjectTypeDefinition)
        {
            Visit(inputObjectTypeDefinition.Directives);
            Visit(inputObjectTypeDefinition.Name);
            Visit(inputObjectTypeDefinition.Fields);
        }

        protected override void VisitTypeExtensionDefinition(GraphQLTypeExtensionDefinition typeExtensionDefinition)
        {
            Visit(typeExtensionDefinition.Definition);
        }

        protected override void VisitDirectiveDefinition(GraphQLDirectiveDefinition directiveDefinition)
        {
            Visit(directiveDefinition.Name);
            Visit(directiveDefinition.Arguments);
            Visit(directiveDefinition.Definitions);
            Visit(directiveDefinition.Locations);
            
        }
    }
}
