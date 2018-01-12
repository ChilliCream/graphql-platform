using GraphQLParser.AST;

namespace Zeus
{
    public partial class SyntaxNodeWalker
        : SyntaxNodeVisitor
    {
        protected SyntaxNodeWalker()
        {
        }

        protected override void VisitArgument(GraphQLArgument argument)
        {
            Visit(argument.Name);
            Visit(argument.Value);
        }
        
        protected override void VisitDirective(GraphQLDirective directive)
        {
            Visit(directive.Name);
            foreach(GraphQLArgument argument in directive.Arguments)
            {
                Visit(argument);
            }
        }
        
        protected override void VisitDocument(GraphQLDocument document)
        {
            foreach(ASTNode node in document.Definitions)
            {
                Visit(node);
            }
        }

        protected override void VisitOperationDefinition(GraphQLOperationDefinition operationDefinition)
        {
            Visit(operationDefinition.Name);
            
            foreach(var x in operationDefinition.Directives)
            {

            }
            base.VisitOperationDefinition(operationDefinition);
        }

        protected override void VisitVariableDefinition(GraphQLVariableDefinition variableDefinition)
        {
            base.VisitVariableDefinition(variableDefinition);
        }

        protected override void VisitVariable(GraphQLVariable variable)
        {
            base.VisitVariable(variable);
        }

        protected override void VisitSelectionSet(GraphQLSelectionSet selectionSet)
        {
            base.VisitSelectionSet(selectionSet);
        }

        protected override void VisitField(GraphQLFieldSelection field)
        {
            base.VisitField(field);
        }

        protected override void VisitFragmentSpread(GraphQLFragmentSpread fragmentSpread)
        {
            base.VisitFragmentSpread(fragmentSpread);
        }

        protected override void VisitInlineFragment(GraphQLInlineFragment inlineFragment)
        {
            base.VisitInlineFragment(inlineFragment);
        }

        protected override void VisitFragmentDefinition(GraphQLFragmentDefinition fragmentDefinition)
        {
            base.VisitFragmentDefinition(fragmentDefinition);
        }

        protected internal override void VisitScalarValue(GraphQLScalarValue scalarValue)
        {
            base.VisitScalarValue(scalarValue);
        }

        protected override void VisitIntValue(GraphQLScalarValue intValue)
        {
            base.VisitIntValue(intValue);
        }

        protected override void VisitFloatValue(GraphQLScalarValue floatValue)
        {
            base.VisitFloatValue(floatValue);
        }

        protected override void VisitStringValue(GraphQLScalarValue stringValue)
        {
            base.VisitStringValue(stringValue);
        }

        protected override void VisitEnumValue(GraphQLScalarValue enumValue)
        {
            base.VisitEnumValue(enumValue);
        }

        protected override void VisitNullValue(GraphQLScalarValue nullValue)
        {
            base.VisitNullValue(nullValue);
        }

        protected override void VisitListValue(GraphQLListValue listValue)
        {
            base.VisitListValue(listValue);
        }

        protected override void VisitObjectValue(GraphQLObjectValue objectValue)
        {
            base.VisitObjectValue(objectValue);
        }

        protected override void VisitObjectField(GraphQLObjectField objectField)
        {
            base.VisitObjectField(objectField);
        }

        protected override void VisitNamedType(GraphQLNamedType namedType)
        {
            base.VisitNamedType(namedType);
        }

        protected override void VisitListType(GraphQLListType listType)
        {
            base.VisitListType(listType);
        }

        protected override void VisitNonNullType(GraphQLNonNullType nonNullType)
        {
            base.VisitNonNullType(nonNullType);
        }

        protected override void VisitSchemaDefinition(GraphQLSchemaDefinition schemaDefinition)
        {
            base.VisitSchemaDefinition(schemaDefinition);
        }

        protected override void VisitOperationTypeDefinition(GraphQLOperationTypeDefinition operationTypeDefinition)
        {
            base.VisitOperationTypeDefinition(operationTypeDefinition);
        }

        protected override void VisitScalarTypeDefinition(GraphQLScalarTypeDefinition scalarTypeDefinition)
        {
            base.VisitScalarTypeDefinition(scalarTypeDefinition);
        }

        protected override void VisitObjectTypeDefinition(GraphQLObjectTypeDefinition objectTypeDefinition)
        {
            base.VisitObjectTypeDefinition(objectTypeDefinition);
        }

        protected override void VisitFieldDefinition(GraphQLFieldDefinition fieldDefinition)
        {
            base.VisitFieldDefinition(fieldDefinition);
        }

        protected override void VisitInputValueDefinition(GraphQLInputValueDefinition inputValueDefinition)
        {
            base.VisitInputValueDefinition(inputValueDefinition);
        }

        protected override void VisitInterfaceTypeDefinition(GraphQLInterfaceTypeDefinition interfaceTypeDefinition)
        {
            base.VisitInterfaceTypeDefinition(interfaceTypeDefinition);
        }

        protected override void VisitUnionTypeDefinition(GraphQLUnionTypeDefinition unionTypeDefinition)
        {
            base.VisitUnionTypeDefinition(unionTypeDefinition);
        }

        protected override void VisitEnumTypeDefinition(GraphQLEnumTypeDefinition enumTypeDefinition)
        {
            base.VisitEnumTypeDefinition(enumTypeDefinition);
        }

        protected override void VisitEnumValueDefinition(GraphQLEnumValueDefinition enumValueDefinition)
        {
            base.VisitEnumValueDefinition(enumValueDefinition);
        }

        protected override void VisitInputObjectTypeDefinition(GraphQLInputObjectTypeDefinition inputObjectTypeDefinition)
        {
            base.VisitInputObjectTypeDefinition(inputObjectTypeDefinition);
        }

        protected override void VisitTypeExtensionDefinition(GraphQLTypeExtensionDefinition typeExtensionDefinition)
        {
            base.VisitTypeExtensionDefinition(typeExtensionDefinition);
        }

        protected override void VisitDirectiveDefinition(GraphQLDirectiveDefinition directiveDefinition)
        {
            base.VisitDirectiveDefinition(directiveDefinition);
        }
    }
}
