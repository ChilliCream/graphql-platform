using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public partial class SyntaxNodeVisitor
    {
        private void ExecuteVisitationMap(ISyntaxNode node)
        {
            switch (node.Kind)
            {
                case NodeKind.Argument:
                    VisitArgument((ArgumentNode)node);
                    break;
                case NodeKind.BooleanValue:
                    VisitBooleanValue((BooleanValueNode)node);
                    break;
                case NodeKind.DirectiveDefinition:
                    VisitDirectiveDefinition((DirectiveDefinitionNode)node);
                    break;
                case NodeKind.Directive:
                    VisitDirective((DirectiveNode)node);
                    break;
                case NodeKind.EnumTypeDefinition:
                    VisitEnumTypeDefinition((EnumTypeDefinitionNode)node);
                    break;
                case NodeKind.EnumTypeExtension:
                    VisitEnumTypeExtension((EnumTypeExtensionNode)node);
                    break;
                case NodeKind.EnumValue:
                    VisitEnumValue((EnumValueNode)node);
                    break;
                case NodeKind.FieldDefinition:
                    VisitFieldDefinition((FieldDefinitionNode)node);
                    break;
                case NodeKind.Field:
                    VisitField((FieldNode)node);
                    break;
                case NodeKind.FloatValue:
                    VisitFloatValue((FloatValueNode)node);
                    break;
                case NodeKind.FragmentDefinition:
                    VisitFragmentDefinition((FragmentDefinitionNode)node);
                    break;
                case NodeKind.FragmentSpread:
                    VisitFragmentSpread((FragmentSpreadNode)node);
                    break;
                case NodeKind.InlineFragment:
                    VisitInlineFragment((InlineFragmentNode)node);
                    break;
                case NodeKind.InputObjectTypeDefinition:
                    VisitInputObjectTypeDefinition((InputObjectTypeDefinitionNode)node);
                    break;
                case NodeKind.InputObjectTypeExtension:
                    VisitInputObjectTypeExtension((InputObjectTypeExtensionNode)node);
                    break;
                case NodeKind.InputValueDefinition:
                    VisitInputValueDefinition((InputValueDefinitionNode)node);
                    break;
                case NodeKind.InterfaceTypeDefinition:
                    VisitInterfaceTypeDefinition((InterfaceTypeDefinitionNode)node);
                    break;
                case NodeKind.InterfaceTypeExtension:
                    VisitInterfaceTypeExtension((InterfaceTypeExtensionNode)node);
                    break;
                case NodeKind.IntValue:
                    VisitIntValue((IntValueNode)node);
                    break;
                case NodeKind.ListType:
                    VisitListType((ListTypeNode)node);
                    break;
                case NodeKind.ListValue:
                    VisitListValue((ListValueNode)node);
                    break;
                case NodeKind.NamedType:
                    VisitNamedType((NamedTypeNode)node);
                    break;
                case NodeKind.Name:
                    VisitName((NameNode)node);
                    break;
                case NodeKind.NonNullType:
                    VisitNonNullType((NonNullTypeNode)node);
                    break;
                case NodeKind.NullValue:
                    VisitNullValue((NullValueNode)node);
                    break;
                case NodeKind.ObjectField:
                    VisitObjectField((ObjectFieldNode)node);
                    break;
                case NodeKind.ObjectTypeDefinition:
                    VisitObjectTypeDefinition((ObjectTypeDefinitionNode)node);
                    break;
                case NodeKind.ObjectTypeExtension:
                    VisitObjectTypeExtension((ObjectTypeExtensionNode)node);
                    break;
                case NodeKind.ObjectValue:
                    VisitObjectValue((ObjectValueNode)node);
                    break;
                case NodeKind.OperationDefinition:
                    VisitOperationDefinition((OperationDefinitionNode)node);
                    break;
                case NodeKind.OperationTypeDefinition:
                    VisitOperationTypeDefinition((OperationTypeDefinitionNode)node);
                    break;
                case NodeKind.ScalarTypeDefinition:
                    VisitScalarTypeDefinition((ScalarTypeDefinitionNode)node);
                    break;
                case NodeKind.ScalarTypeExtension:
                    VisitScalarTypeExtension((ScalarTypeExtensionNode)node);
                    break;
                case NodeKind.SchemaDefinition:
                    VisitSchemaDefinition((SchemaDefinitionNode)node);
                    break;
                case NodeKind.SelectionSet:
                    VisitSelectionSet((SelectionSetNode)node);
                    break;
                case NodeKind.StringValue:
                    VisitStringValue((StringValueNode)node);
                    break;
                case NodeKind.UnionTypeDefinition:
                    VisitUnionTypeDefinition((UnionTypeDefinitionNode)node);
                    break;
                case NodeKind.UnionTypeExtension:
                    VisitUnionTypeExtension((UnionTypeExtensionNode)node);
                    break;
                case NodeKind.VariableDefinition:
                    VisitVariableDefinition((VariableDefinitionNode)node);
                    break;
                case NodeKind.Variable:
                    VisitVariable((VariableNode)node);
                    break;
                default:
                    throw new NotSupportedException(
                        $"The specified node kind {node.Kind} is not yet supported.");
            }
        }
    }
}