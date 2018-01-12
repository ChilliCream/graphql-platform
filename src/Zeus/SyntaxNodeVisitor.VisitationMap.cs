using System;
using System.Collections.Generic;
using GraphQLParser.AST;

namespace Zeus
{
    public partial class SyntaxNodeVisitor
    {
        private static readonly Dictionary<ASTNodeKind, Action<SyntaxNodeVisitor, ASTNode>> _visitationMap
         = new Dictionary<ASTNodeKind, Action<SyntaxNodeVisitor, ASTNode>>
         {
                { ASTNodeKind.Argument, (v, n) => v.VisitArgument((GraphQLArgument)n) },
                { ASTNodeKind.Directive, (v, n) => v.VisitDirective((GraphQLDirective)n) },
                { ASTNodeKind.DirectiveDefinition, (v, n) => v.VisitDirectiveDefinition((GraphQLDirectiveDefinition)n) },
                { ASTNodeKind.Document, (v, n) => v.VisitDocument((GraphQLDocument)n) },
                { ASTNodeKind.EnumTypeDefinition, (v, n) => v.VisitEnumTypeDefinition((GraphQLEnumTypeDefinition)n) },
                { ASTNodeKind.EnumValueDefinition, (v, n) => v.VisitEnumValueDefinition((GraphQLEnumValueDefinition)n) },
                { ASTNodeKind.Field, (v, n) => v.VisitField((GraphQLFieldSelection)n) },
                { ASTNodeKind.FieldDefinition, (v, n) => v.VisitFieldDefinition((GraphQLFieldDefinition)n) },
                { ASTNodeKind.FragmentDefinition, (v, n) => v.VisitFragmentDefinition((GraphQLFragmentDefinition)n) },
                { ASTNodeKind.FragmentSpread, (v, n) => v.VisitFragmentSpread((GraphQLFragmentSpread)n) },
                { ASTNodeKind.InlineFragment, (v, n) => v.VisitInlineFragment((GraphQLInlineFragment)n) },
                { ASTNodeKind.InputObjectTypeDefinition, (v, n) => v.VisitInputObjectTypeDefinition((GraphQLInputObjectTypeDefinition)n) },
                { ASTNodeKind.InputValueDefinition, (v, n) => v.VisitInputValueDefinition((GraphQLInputValueDefinition)n) },
                { ASTNodeKind.InterfaceTypeDefinition, (v, n) => v.VisitInterfaceTypeDefinition((GraphQLInterfaceTypeDefinition)n) },
                { ASTNodeKind.ListType, (v, n) => v.VisitListType((GraphQLListType)n) },
                { ASTNodeKind.ListValue, (v, n) => v.VisitListValue((GraphQLListValue)n) },
                { ASTNodeKind.Name, (v, n) => v.VisitName((GraphQLName)n) },
                { ASTNodeKind.NamedType, (v, n) => v.VisitNamedType((GraphQLNamedType)n) },
                { ASTNodeKind.NonNullType, (v, n) => v.VisitNonNullType((GraphQLNonNullType)n) },
                { ASTNodeKind.ObjectField, (v, n) => v.VisitObjectField((GraphQLObjectField)n) },
                { ASTNodeKind.ObjectTypeDefinition, (v, n) => v.VisitObjectTypeDefinition((GraphQLObjectTypeDefinition)n) },
                { ASTNodeKind.ObjectValue, (v, n) => v.VisitObjectValue((GraphQLObjectValue)n) },
                { ASTNodeKind.OperationDefinition, (v, n) => v.VisitOperationDefinition((GraphQLOperationDefinition)n) },
                { ASTNodeKind.OperationTypeDefinition, (v, n) => v.VisitOperationTypeDefinition((GraphQLOperationTypeDefinition)n) },
                { ASTNodeKind.ScalarTypeDefinition, (v, n) => v.VisitScalarTypeDefinition((GraphQLScalarTypeDefinition)n) },
                { ASTNodeKind.SchemaDefinition, (v, n) => v.VisitSchemaDefinition((GraphQLSchemaDefinition)n) },
                { ASTNodeKind.SelectionSet, (v, n) => v.VisitSelectionSet((GraphQLSelectionSet)n) },
                { ASTNodeKind.TypeExtensionDefinition, (v, n) => v.VisitTypeExtensionDefinition((GraphQLTypeExtensionDefinition)n) },
                { ASTNodeKind.UnionTypeDefinition, (v, n) => v.VisitUnionTypeDefinition((GraphQLUnionTypeDefinition)n) },
                { ASTNodeKind.Variable, (v, n) => v.VisitVariable((GraphQLVariable)n) },
                { ASTNodeKind.VariableDefinition, (v, n) => v.VisitVariableDefinition((GraphQLVariableDefinition)n) },

                // scalar values
                { ASTNodeKind.EnumValue, (v, n) => v.VisitScalarValue((GraphQLScalarValue)n) },
                { ASTNodeKind.BooleanValue, (v, n) => v.VisitScalarValue((GraphQLScalarValue)n) },
                { ASTNodeKind.FloatValue, (v, n) => v.VisitScalarValue((GraphQLScalarValue)n) },
                { ASTNodeKind.IntValue, (v, n) => v.VisitScalarValue((GraphQLScalarValue)n) },
                { ASTNodeKind.NullValue, (v, n) => v.VisitScalarValue((GraphQLScalarValue)n) },
                { ASTNodeKind.StringValue, (v, n) => v.VisitScalarValue((GraphQLScalarValue)n) },
         };
    }
}
