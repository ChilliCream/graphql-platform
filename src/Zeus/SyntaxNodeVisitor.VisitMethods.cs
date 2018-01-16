using System;
using System.Collections.Generic;
using System.Text;
using GraphQLParser.AST;

namespace Zeus
{
    public partial class SyntaxNodeVisitor
    {
        protected virtual void VisitName(GraphQLName name) { }
        protected virtual void VisitDocument(GraphQLDocument document) { }
        protected virtual void VisitOperationDefinition(GraphQLOperationDefinition operationDefinition) { }
        protected virtual void VisitVariableDefinition(GraphQLVariableDefinition variableDefinition) { }
        protected virtual void VisitVariable(GraphQLVariable variable) { }
        protected virtual void VisitSelectionSet(GraphQLSelectionSet selectionSet) { }
        protected virtual void VisitField(GraphQLFieldSelection field) { }
        protected virtual void VisitArgument(GraphQLArgument argument) { }
        protected virtual void VisitFragmentSpread(GraphQLFragmentSpread fragmentSpread) { }
        protected virtual void VisitInlineFragment(GraphQLInlineFragment inlineFragment) { }
        protected virtual void VisitFragmentDefinition(GraphQLFragmentDefinition fragmentDefinition) { }

        internal protected virtual void VisitScalarValue(GraphQLScalarValue scalarValue)
        {
            switch (scalarValue.Kind)
            {
                case ASTNodeKind.IntValue:
                    VisitIntValue(scalarValue);
                    break;
                case ASTNodeKind.FloatValue:
                    VisitFloatValue(scalarValue);
                    break;
                case ASTNodeKind.StringValue:
                    VisitStringValue(scalarValue);
                    break;
                case ASTNodeKind.BooleanValue:
                    VisitBooleanValue(scalarValue);
                    break;
                case ASTNodeKind.EnumValue:
                    VisitEnumValue(scalarValue);
                    break;
                case ASTNodeKind.NullValue:
                    VisitEnumValue(scalarValue);
                    break;

                default:
                    throw new InvalidOperationException();
            }
        }

        protected virtual void VisitIntValue(GraphQLScalarValue intValue) { }
        protected virtual void VisitFloatValue(GraphQLScalarValue floatValue) { }
        protected virtual void VisitStringValue(GraphQLScalarValue stringValue) { }
        protected virtual void VisitBooleanValue(GraphQLScalarValue booleanValue) { }
        protected virtual void VisitEnumValue(GraphQLScalarValue enumValue) { }
        protected virtual void VisitNullValue(GraphQLScalarValue nullValue) { }

        protected virtual void VisitListValue(GraphQLListValue listValue) { }
        protected virtual void VisitObjectValue(GraphQLObjectValue objectValue) { }
        protected virtual void VisitObjectField(GraphQLObjectField objectField) { }
        protected virtual void VisitDirective(GraphQLDirective directive) { }
        protected virtual void VisitNamedType(GraphQLNamedType namedType) { }
        protected virtual void VisitListType(GraphQLListType listType) { }
        protected virtual void VisitNonNullType(GraphQLNonNullType nonNullType) { }
        protected virtual void VisitSchemaDefinition(GraphQLSchemaDefinition schemaDefinition) { }
        protected virtual void VisitOperationTypeDefinition(GraphQLOperationTypeDefinition operationTypeDefinition) { }
        protected virtual void VisitScalarTypeDefinition(GraphQLScalarTypeDefinition scalarTypeDefinition) { }
        protected virtual void VisitObjectTypeDefinition(GraphQLObjectTypeDefinition objectTypeDefinition) { }
        protected virtual void VisitFieldDefinition(GraphQLFieldDefinition fieldDefinition) { }
        protected virtual void VisitInputValueDefinition(GraphQLInputValueDefinition inputValueDefinition) { }
        protected virtual void VisitInterfaceTypeDefinition(GraphQLInterfaceTypeDefinition interfaceTypeDefinition) { }
        protected virtual void VisitUnionTypeDefinition(GraphQLUnionTypeDefinition unionTypeDefinition) { }
        protected virtual void VisitEnumTypeDefinition(GraphQLEnumTypeDefinition enumTypeDefinition) { }
        protected virtual void VisitEnumValueDefinition(GraphQLEnumValueDefinition enumValueDefinition) { }
        protected virtual void VisitInputObjectTypeDefinition(GraphQLInputObjectTypeDefinition inputObjectTypeDefinition) { }
        protected virtual void VisitTypeExtensionDefinition(GraphQLTypeExtensionDefinition typeExtensionDefinition) { }
        protected virtual void VisitDirectiveDefinition(GraphQLDirectiveDefinition directiveDefinition) { }
    }

    public partial class SyntaxNodeVisitor<TContext>
    {
        protected virtual void VisitName(GraphQLName name, TContext context) { }
        protected virtual void VisitDocument(GraphQLDocument document, TContext context) { }
        protected virtual void VisitOperationDefinition(GraphQLOperationDefinition operationDefinition, TContext context) { }
        protected virtual void VisitVariableDefinition(GraphQLVariableDefinition variableDefinition, TContext context) { }
        protected virtual void VisitVariable(GraphQLVariable variable, TContext context) { }
        protected virtual void VisitSelectionSet(GraphQLSelectionSet selectionSet, TContext context) { }
        protected virtual void VisitField(GraphQLFieldSelection field, TContext context) { }
        protected virtual void VisitArgument(GraphQLArgument argument, TContext context) { }
        protected virtual void VisitFragmentSpread(GraphQLFragmentSpread fragmentSpread, TContext context) { }
        protected virtual void VisitInlineFragment(GraphQLInlineFragment inlineFragment, TContext context) { }
        protected virtual void VisitFragmentDefinition(GraphQLFragmentDefinition fragmentDefinition, TContext context) { }

        internal protected virtual void VisitScalarValue(GraphQLScalarValue scalarValue, TContext context)
        {
            switch (scalarValue.Kind)
            {
                case ASTNodeKind.IntValue:
                    VisitIntValue(scalarValue, context);
                    break;
                case ASTNodeKind.FloatValue:
                    VisitFloatValue(scalarValue, context);
                    break;
                case ASTNodeKind.StringValue:
                    VisitStringValue(scalarValue, context);
                    break;
                case ASTNodeKind.BooleanValue:
                    VisitBooleanValue(scalarValue, context);
                    break;
                case ASTNodeKind.EnumValue:
                    VisitEnumValue(scalarValue, context);
                    break;
                case ASTNodeKind.NullValue:
                    VisitEnumValue(scalarValue, context);
                    break;

                default:
                    throw new InvalidOperationException();
            }
        }

        protected virtual void VisitIntValue(GraphQLScalarValue intValue, TContext context) { }
        protected virtual void VisitFloatValue(GraphQLScalarValue floatValue, TContext context) { }
        protected virtual void VisitStringValue(GraphQLScalarValue stringValue, TContext context) { }
        protected virtual void VisitBooleanValue(GraphQLScalarValue booleanValue, TContext context) { }
        protected virtual void VisitEnumValue(GraphQLScalarValue enumValue, TContext context) { }
        protected virtual void VisitNullValue(GraphQLScalarValue nullValue, TContext context) { }

        protected virtual void VisitListValue(GraphQLListValue listValue, TContext context) { }
        protected virtual void VisitObjectValue(GraphQLObjectValue objectValue, TContext context) { }
        protected virtual void VisitObjectField(GraphQLObjectField objectField, TContext context) { }
        protected virtual void VisitDirective(GraphQLDirective directive, TContext context) { }
        protected virtual void VisitNamedType(GraphQLNamedType namedType, TContext context) { }
        protected virtual void VisitListType(GraphQLListType listType, TContext context) { }
        protected virtual void VisitNonNullType(GraphQLNonNullType nonNullType, TContext context) { }
        protected virtual void VisitSchemaDefinition(GraphQLSchemaDefinition schemaDefinition, TContext context) { }
        protected virtual void VisitOperationTypeDefinition(GraphQLOperationTypeDefinition operationTypeDefinition, TContext context) { }
        protected virtual void VisitScalarTypeDefinition(GraphQLScalarTypeDefinition scalarTypeDefinition, TContext context) { }
        protected virtual void VisitObjectTypeDefinition(GraphQLObjectTypeDefinition objectTypeDefinition, TContext context) { }
        protected virtual void VisitFieldDefinition(GraphQLFieldDefinition fieldDefinition, TContext context) { }
        protected virtual void VisitInputValueDefinition(GraphQLInputValueDefinition inputValueDefinition, TContext context) { }
        protected virtual void VisitInterfaceTypeDefinition(GraphQLInterfaceTypeDefinition interfaceTypeDefinition, TContext context) { }
        protected virtual void VisitUnionTypeDefinition(GraphQLUnionTypeDefinition unionTypeDefinition, TContext context) { }
        protected virtual void VisitEnumTypeDefinition(GraphQLEnumTypeDefinition enumTypeDefinition, TContext context) { }
        protected virtual void VisitEnumValueDefinition(GraphQLEnumValueDefinition enumValueDefinition, TContext context) { }
        protected virtual void VisitInputObjectTypeDefinition(GraphQLInputObjectTypeDefinition inputObjectTypeDefinition, TContext context) { }
        protected virtual void VisitTypeExtensionDefinition(GraphQLTypeExtensionDefinition typeExtensionDefinition, TContext context) { }
        protected virtual void VisitDirectiveDefinition(GraphQLDirectiveDefinition directiveDefinition, TContext context) { }
    }
}
