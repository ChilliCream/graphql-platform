using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    // TODO : we are still missing some visitations

    public partial class SyntaxNodeVisitor
    {
        private static readonly Dictionary<NodeKind, Action<SyntaxNodeVisitor, ISyntaxNode>> _visitationMap
         = new Dictionary<NodeKind, Action<SyntaxNodeVisitor, ISyntaxNode>>
         {
                { NodeKind.Argument, (v, n) => v.VisitArgument((ArgumentNode)n) },
                { NodeKind.Directive, (v, n) => v.VisitDirective((DirectiveNode)n) },
                { NodeKind.DirectiveDefinition, (v, n) => v.VisitDirectiveDefinition((DirectiveDefinitionNode)n) },
                { NodeKind.Document, (v, n) => v.VisitDocument((DocumentNode)n) },
                { NodeKind.EnumTypeDefinition, (v, n) => v.VisitEnumTypeDefinition((EnumTypeDefinitionNode)n) },
                { NodeKind.EnumValueDefinition, (v, n) => v.VisitEnumValueDefinition((EnumValueDefinitionNode)n) },
                { NodeKind.Field, (v, n) => v.VisitField((FieldNode)n) },
                { NodeKind.FieldDefinition, (v, n) => v.VisitFieldDefinition((FieldDefinitionNode)n) },
                { NodeKind.FragmentDefinition, (v, n) => v.VisitFragmentDefinition((FragmentDefinitionNode)n) },
                { NodeKind.FragmentSpread, (v, n) => v.VisitFragmentSpread((FragmentSpreadNode)n) },
                { NodeKind.InlineFragment, (v, n) => v.VisitInlineFragment((InlineFragmentNode)n) },
                { NodeKind.InputObjectTypeDefinition, (v, n) => v.VisitInputObjectTypeDefinition((InputObjectTypeDefinitionNode)n) },
                { NodeKind.InputValueDefinition, (v, n) => v.VisitInputValueDefinition((InputValueDefinitionNode)n) },
                { NodeKind.InterfaceTypeDefinition, (v, n) => v.VisitInterfaceTypeDefinition((InterfaceTypeDefinitionNode)n) },
                { NodeKind.ListType, (v, n) => v.VisitListType((ListTypeNode)n) },
                { NodeKind.ListValue, (v, n) => v.VisitListValue((ListValueNode)n) },
                { NodeKind.Name, (v, n) => v.VisitName((NameNode)n) },
                { NodeKind.NamedType, (v, n) => v.VisitNamedType((NamedTypeNode)n) },
                { NodeKind.NonNullType, (v, n) => v.VisitNonNullType((NonNullTypeNode)n) },
                { NodeKind.ObjectField, (v, n) => v.VisitObjectField((ObjectFieldNode)n) },
                { NodeKind.ObjectTypeDefinition, (v, n) => v.VisitObjectTypeDefinition((ObjectTypeDefinitionNode)n) },
                { NodeKind.ObjectValue, (v, n) => v.VisitObjectValue((ObjectValueNode)n) },
                { NodeKind.OperationDefinition, (v, n) => v.VisitOperationDefinition((OperationDefinitionNode)n) },
                { NodeKind.OperationTypeDefinition, (v, n) => v.VisitOperationTypeDefinition((OperationTypeDefinitionNode)n) },
                { NodeKind.ScalarTypeDefinition, (v, n) => v.VisitScalarTypeDefinition((ScalarTypeDefinitionNode)n) },
                { NodeKind.SchemaDefinition, (v, n) => v.VisitSchemaDefinition((SchemaDefinitionNode)n) },
                { NodeKind.SelectionSet, (v, n) => v.VisitSelectionSet((SelectionSetNode)n) },
                { NodeKind.UnionTypeDefinition, (v, n) => v.VisitUnionTypeDefinition((UnionTypeDefinitionNode)n) },
                { NodeKind.Variable, (v, n) => v.VisitVariable((VariableNode)n) },
                { NodeKind.VariableDefinition, (v, n) => v.VisitVariableDefinition((VariableDefinitionNode)n) },
                { NodeKind.EnumValue, (v, n) => v.VisitEnumValue((EnumValueNode)n) },
                { NodeKind.BooleanValue, (v, n) => v.VisitBooleanValue((BooleanValueNode)n) },
                { NodeKind.FloatValue, (v, n) => v.VisitFloatValue((FloatValueNode)n) },
                { NodeKind.IntValue, (v, n) => v.VisitIntValue((IntValueNode)n) },
                { NodeKind.NullValue, (v, n) => v.VisitNullValue((NullValueNode)n) },
                { NodeKind.StringValue, (v, n) => v.VisitStringValue((StringValueNode)n) },
         };
    }
}