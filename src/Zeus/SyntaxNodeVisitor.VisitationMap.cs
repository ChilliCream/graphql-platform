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

    public partial class SyntaxNodeVisitor<TContext>
    {
        private static readonly Dictionary<ASTNodeKind, Action<SyntaxNodeVisitor<TContext>, ASTNode, TContext>> _visitationMap
         = new Dictionary<ASTNodeKind, Action<SyntaxNodeVisitor<TContext>, ASTNode, TContext>>
         {
                { ASTNodeKind.Argument, (v, n, c) => v.VisitArgument((GraphQLArgument)n, c) },
                { ASTNodeKind.Directive, (v, n, c) => v.VisitDirective((GraphQLDirective)n, c) },
                { ASTNodeKind.DirectiveDefinition, (v, n, c) => v.VisitDirectiveDefinition((GraphQLDirectiveDefinition)n, c) },
                { ASTNodeKind.Document, (v, n, c) => v.VisitDocument((GraphQLDocument)n, c) },
                { ASTNodeKind.EnumTypeDefinition, (v, n, c) => v.VisitEnumTypeDefinition((GraphQLEnumTypeDefinition)n, c) },
                { ASTNodeKind.EnumValueDefinition, (v, n, c) => v.VisitEnumValueDefinition((GraphQLEnumValueDefinition)n, c) },
                { ASTNodeKind.Field, (v, n, c) => v.VisitField((GraphQLFieldSelection)n, c) },
                { ASTNodeKind.FieldDefinition, (v, n, c) => v.VisitFieldDefinition((GraphQLFieldDefinition)n, c) },
                { ASTNodeKind.FragmentDefinition, (v, n, c) => v.VisitFragmentDefinition((GraphQLFragmentDefinition)n, c) },
                { ASTNodeKind.FragmentSpread, (v, n, c) => v.VisitFragmentSpread((GraphQLFragmentSpread)n, c) },
                { ASTNodeKind.InlineFragment, (v, n, c) => v.VisitInlineFragment((GraphQLInlineFragment)n, c) },
                { ASTNodeKind.InputObjectTypeDefinition, (v, n, c) => v.VisitInputObjectTypeDefinition((GraphQLInputObjectTypeDefinition)n, c) },
                { ASTNodeKind.InputValueDefinition, (v, n, c) => v.VisitInputValueDefinition((GraphQLInputValueDefinition)n, c) },
                { ASTNodeKind.InterfaceTypeDefinition, (v, n, c) => v.VisitInterfaceTypeDefinition((GraphQLInterfaceTypeDefinition)n, c) },
                { ASTNodeKind.ListType, (v, n, c) => v.VisitListType((GraphQLListType)n, c) },
                { ASTNodeKind.ListValue, (v, n, c) => v.VisitListValue((GraphQLListValue)n, c) },
                { ASTNodeKind.Name, (v, n, c) => v.VisitName((GraphQLName)n, c) },
                { ASTNodeKind.NamedType, (v, n, c) => v.VisitNamedType((GraphQLNamedType)n, c) },
                { ASTNodeKind.NonNullType, (v, n, c) => v.VisitNonNullType((GraphQLNonNullType)n, c) },
                { ASTNodeKind.ObjectField, (v, n, c) => v.VisitObjectField((GraphQLObjectField)n, c) },
                { ASTNodeKind.ObjectTypeDefinition, (v, n, c) => v.VisitObjectTypeDefinition((GraphQLObjectTypeDefinition)n, c) },
                { ASTNodeKind.ObjectValue, (v, n, c) => v.VisitObjectValue((GraphQLObjectValue)n, c) },
                { ASTNodeKind.OperationDefinition, (v, n, c) => v.VisitOperationDefinition((GraphQLOperationDefinition)n, c) },
                { ASTNodeKind.OperationTypeDefinition, (v, n, c) => v.VisitOperationTypeDefinition((GraphQLOperationTypeDefinition)n, c) },
                { ASTNodeKind.ScalarTypeDefinition, (v, n, c) => v.VisitScalarTypeDefinition((GraphQLScalarTypeDefinition)n, c) },
                { ASTNodeKind.SchemaDefinition, (v, n, c) => v.VisitSchemaDefinition((GraphQLSchemaDefinition)n, c) },
                { ASTNodeKind.SelectionSet, (v, n, c) => v.VisitSelectionSet((GraphQLSelectionSet)n, c) },
                { ASTNodeKind.TypeExtensionDefinition, (v, n, c) => v.VisitTypeExtensionDefinition((GraphQLTypeExtensionDefinition)n, c) },
                { ASTNodeKind.UnionTypeDefinition, (v, n, c) => v.VisitUnionTypeDefinition((GraphQLUnionTypeDefinition)n, c) },
                { ASTNodeKind.Variable, (v, n, c) => v.VisitVariable((GraphQLVariable)n, c) },
                { ASTNodeKind.VariableDefinition, (v, n, c) => v.VisitVariableDefinition((GraphQLVariableDefinition)n, c) },

                // scalar values
                { ASTNodeKind.EnumValue, (v, n, c) => v.VisitScalarValue((GraphQLScalarValue)n, c) },
                { ASTNodeKind.BooleanValue, (v, n, c) => v.VisitScalarValue((GraphQLScalarValue)n, c) },
                { ASTNodeKind.FloatValue, (v, n, c) => v.VisitScalarValue((GraphQLScalarValue)n, c) },
                { ASTNodeKind.IntValue, (v, n, c) => v.VisitScalarValue((GraphQLScalarValue)n, c) },
                { ASTNodeKind.NullValue, (v, n, c) => v.VisitScalarValue((GraphQLScalarValue)n, c) },
                { ASTNodeKind.StringValue, (v, n, c) => v.VisitScalarValue((GraphQLScalarValue)n, c) },
         };
    }
}
