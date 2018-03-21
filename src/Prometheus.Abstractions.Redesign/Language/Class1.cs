using System;

namespace Prometheus.Language
{

    public interface ISyntaxNode
    {
        NodeKind Kind { get; }
        Location Location { get; }
    }

    public class Token
    {

    }

    public class Source { }

    public class Location
    {
        /// <summary>
        /// Gets the character offset at which this <see cref="ISyntaxNode" /> begins.
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// Gets the character offset at which this <see cref="ISyntaxNode" /> ends.
        /// </summary>
        public int End { get; }


        /// <summary>
        /// Gets the <see cref="Token" /> at which this <see cref="ISyntaxNode" /> begins.
        /// </summary>
        public Token StartToken { get; }

        /// <summary>
        /// Gets the <see cref="Token" /> at which this <see cref="ISyntaxNode" /> ends.
        /// </summary>
        public Token EndToken { get; }

        /// <summary>
        /// Gets the <see cref="Source" /> document the AST represents.
        /// </summary>
        /// <returns></returns>
        public Source Source { get; }

    }


    public enum NodeKind
    {
        Name,
        Document,
        OperationDefinition,
        VariableDefinition,
        Variable,
        SelectionSet
    }

    public class NameNode
        : ISyntaxNode
    {
        public NodeKind Kind { get; } = NodeKind.Name;
        public Location Location { get; }
        public string Value { get; }
    }

    // Document

    public class DocumentNode
        : ISyntaxNode
    {
        public NodeKind Kind { get; } = NodeKind.Document;
        public Location Location { get; }
        public IReadOnlyCollection<IExecutableDefinitionNode> Definitions { get; }
    }

    public interface IExecutableDefinitionNode
        : ISyntaxNode
    {
        NameNode Name { get; }
    }

    public enum OperationTypeNode
    {
        Query,
        Mutation,
        Subscription
    }

    public class OperationDefinitionNode
        : IExecutableDefinitionNode
    {
        public NodeKind Kind { get; } = NodeKind.OperationDefinition;
        public Location Location { get; }
        public NameNode Name { get; }
        public OperationTypeNode Operation { get; }
        public IReadOnlyCollection<VariableDefinitionNode> VariableDefinitions { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public SelectionSetNode SelectionSet { get; }
    }


    public class VariableDefinitionNode
        : ISyntaxNode
    {
        public NodeKind Kind { get; } = NodeKind.VariableDefinition;
        public Location Location { get; }
        public VariableNode Variable { get; }
        public TypeNode Type { get; }
        public ValueNode DefaultValue { get; }
    }

    public class DirectiveNode
    {

    }

    public class SelectionSetNode
        : ISyntaxNode
    {
        public NodeKind Kind { get; } = NodeKind.SelectionSet;
        public IReadOnlyCollection<ISelectionNode> Selections { get; }
    }

    public class VariableNode
        : ISyntaxNode
    {
        public NodeKind Kind { get; } = NodeKind.Variable;
        public Location Location { get; }
        public NameNode Name { get; }
    }

    public class TypeNode
    {

    }

    public class ValueNode
    {

    }

    public interface ISelectionNode
        : ISyntaxNode
    {

    }

    export type SelectionSetNode = {
  kind: 'SelectionSet',
  loc?: Location,
  selections: $ReadOnlyArray<SelectionNode>,
};

export type SelectionNode = FieldNode | FragmentSpreadNode | InlineFragmentNode;

export type FieldNode = {
  +kind: 'Field',
  +loc?: Location,
  +alias?: NameNode,
  +name: NameNode,
  +arguments?: $ReadOnlyArray<ArgumentNode>,
  +directives?: $ReadOnlyArray<DirectiveNode>,
  +selectionSet?: SelectionSetNode,
};

export type ArgumentNode = {
  +kind: 'Argument',
  +loc?: Location,
  +name: NameNode,
  +value: ValueNode,
};

// Fragments

export type FragmentSpreadNode = {
  +kind: 'FragmentSpread',
  +loc?: Location,
  +name: NameNode,
  +directives?: $ReadOnlyArray<DirectiveNode>,
};

export type InlineFragmentNode = {
  +kind: 'InlineFragment',
  +loc?: Location,
  +typeCondition?: NamedTypeNode,
  +directives?: $ReadOnlyArray<DirectiveNode>,
  +selectionSet: SelectionSetNode,
};

export type FragmentDefinitionNode = {
  +kind: 'FragmentDefinition',
  +loc?: Location,
  +name: NameNode,
  // Note: fragment variable definitions are experimental and may be changed
  // or removed in the future.
  +variableDefinitions?: $ReadOnlyArray<VariableDefinitionNode>,
  +typeCondition: NamedTypeNode,
  +directives?: $ReadOnlyArray<DirectiveNode>,
  +selectionSet: SelectionSetNode,
};

// Values

export type ValueNode =
  | VariableNode
  | IntValueNode
  | FloatValueNode
  | StringValueNode
  | BooleanValueNode
  | NullValueNode
  | EnumValueNode
  | ListValueNode
  | ObjectValueNode;

export type IntValueNode = {
  +kind: 'IntValue',
  +loc?: Location,
  +value: string,
};

export type FloatValueNode = {
  +kind: 'FloatValue',
  +loc?: Location,
  +value: string,
};

export type StringValueNode = {
  +kind: 'StringValue',
  +loc?: Location,
  +value: string,
  +block?: boolean,
};

export type BooleanValueNode = {
  +kind: 'BooleanValue',
  +loc?: Location,
  +value: boolean,
};

export type NullValueNode = {
  +kind: 'NullValue',
  +loc?: Location,
};

export type EnumValueNode = {
  +kind: 'EnumValue',
  +loc?: Location,
  +value: string,
};

export type ListValueNode = {
  +kind: 'ListValue',
  +loc?: Location,
  +values: $ReadOnlyArray<ValueNode>,
};

export type ObjectValueNode = {
  +kind: 'ObjectValue',
  +loc?: Location,
  +fields: $ReadOnlyArray<ObjectFieldNode>,
};

export type ObjectFieldNode = {
  +kind: 'ObjectField',
  +loc?: Location,
  +name: NameNode,
  +value: ValueNode,
};

// Directives

export type DirectiveNode = {
  +kind: 'Directive',
  +loc?: Location,
  +name: NameNode,
  +arguments?: $ReadOnlyArray<ArgumentNode>,
};

// Type Reference

export type TypeNode = NamedTypeNode | ListTypeNode | NonNullTypeNode;

export type NamedTypeNode = {
  +kind: 'NamedType',
  +loc?: Location,
  +name: NameNode,
};

export type ListTypeNode = {
  +kind: 'ListType',
  +loc?: Location,
  +type: TypeNode,
};

export type NonNullTypeNode = {
  +kind: 'NonNullType',
  +loc?: Location,
  +type: NamedTypeNode | ListTypeNode,
};

// Type System Definition

export type TypeSystemDefinitionNode =
  | SchemaDefinitionNode
  | TypeDefinitionNode
  | TypeExtensionNode
  | DirectiveDefinitionNode;

export type SchemaDefinitionNode = {
  +kind: 'SchemaDefinition',
  +loc?: Location,
  +directives: $ReadOnlyArray<DirectiveNode>,
  +operationTypes: $ReadOnlyArray<OperationTypeDefinitionNode>,
};

export type OperationTypeDefinitionNode = {
  +kind: 'OperationTypeDefinition',
  +loc?: Location,
  +operation: OperationTypeNode,
  +type: NamedTypeNode,
};

// Type Definition

export type TypeDefinitionNode =
  | ScalarTypeDefinitionNode
  | ObjectTypeDefinitionNode
  | InterfaceTypeDefinitionNode
  | UnionTypeDefinitionNode
  | EnumTypeDefinitionNode
  | InputObjectTypeDefinitionNode;

export type ScalarTypeDefinitionNode = {
  +kind: 'ScalarTypeDefinition',
  +loc?: Location,
  +description?: StringValueNode,
  +name: NameNode,
  +directives?: $ReadOnlyArray<DirectiveNode>,
};

export type ObjectTypeDefinitionNode = {
  +kind: 'ObjectTypeDefinition',
  +loc?: Location,
  +description?: StringValueNode,
  +name: NameNode,
  +interfaces?: $ReadOnlyArray<NamedTypeNode>,
  +directives?: $ReadOnlyArray<DirectiveNode>,
  +fields?: $ReadOnlyArray<FieldDefinitionNode>,
};

export type FieldDefinitionNode = {
  +kind: 'FieldDefinition',
  +loc?: Location,
  +description?: StringValueNode,
  +name: NameNode,
  +arguments?: $ReadOnlyArray<InputValueDefinitionNode>,
  +type: TypeNode,
  +directives?: $ReadOnlyArray<DirectiveNode>,
};

export type InputValueDefinitionNode = {
  +kind: 'InputValueDefinition',
  +loc?: Location,
  +description?: StringValueNode,
  +name: NameNode,
  +type: TypeNode,
  +defaultValue?: ValueNode,
  +directives?: $ReadOnlyArray<DirectiveNode>,
};

export type InterfaceTypeDefinitionNode = {
  +kind: 'InterfaceTypeDefinition',
  +loc?: Location,
  +description?: StringValueNode,
  +name: NameNode,
  +directives?: $ReadOnlyArray<DirectiveNode>,
  +fields?: $ReadOnlyArray<FieldDefinitionNode>,
};

export type UnionTypeDefinitionNode = {
  +kind: 'UnionTypeDefinition',
  +loc?: Location,
  +description?: StringValueNode,
  +name: NameNode,
  +directives?: $ReadOnlyArray<DirectiveNode>,
  +types?: $ReadOnlyArray<NamedTypeNode>,
};

export type EnumTypeDefinitionNode = {
  +kind: 'EnumTypeDefinition',
  +loc?: Location,
  +description?: StringValueNode,
  +name: NameNode,
  +directives?: $ReadOnlyArray<DirectiveNode>,
  +values?: $ReadOnlyArray<EnumValueDefinitionNode>,
};

export type EnumValueDefinitionNode = {
  +kind: 'EnumValueDefinition',
  +loc?: Location,
  +description?: StringValueNode,
  +name: NameNode,
  +directives?: $ReadOnlyArray<DirectiveNode>,
};

export type InputObjectTypeDefinitionNode = {
  +kind: 'InputObjectTypeDefinition',
  +loc?: Location,
  +description?: StringValueNode,
  +name: NameNode,
  +directives?: $ReadOnlyArray<DirectiveNode>,
  +fields?: $ReadOnlyArray<InputValueDefinitionNode>,
};

// Type Extensions

export type TypeExtensionNode =
  | ScalarTypeExtensionNode
  | ObjectTypeExtensionNode
  | InterfaceTypeExtensionNode
  | UnionTypeExtensionNode
  | EnumTypeExtensionNode
  | InputObjectTypeExtensionNode;

export type ScalarTypeExtensionNode = {
  +kind: 'ScalarTypeExtension',
  +loc?: Location,
  +name: NameNode,
  +directives?: $ReadOnlyArray<DirectiveNode>,
};

export type ObjectTypeExtensionNode = {
  +kind: 'ObjectTypeExtension',
  +loc?: Location,
  +name: NameNode,
  +interfaces?: $ReadOnlyArray<NamedTypeNode>,
  +directives?: $ReadOnlyArray<DirectiveNode>,
  +fields?: $ReadOnlyArray<FieldDefinitionNode>,
};

export type InterfaceTypeExtensionNode = {
  +kind: 'InterfaceTypeExtension',
  +loc?: Location,
  +name: NameNode,
  +directives?: $ReadOnlyArray<DirectiveNode>,
  +fields?: $ReadOnlyArray<FieldDefinitionNode>,
};

export type UnionTypeExtensionNode = {
  +kind: 'UnionTypeExtension',
  +loc?: Location,
  +name: NameNode,
  +directives?: $ReadOnlyArray<DirectiveNode>,
  +types?: $ReadOnlyArray<NamedTypeNode>,
};

export type EnumTypeExtensionNode = {
  +kind: 'EnumTypeExtension',
  +loc?: Location,
  +name: NameNode,
  +directives?: $ReadOnlyArray<DirectiveNode>,
  +values?: $ReadOnlyArray<EnumValueDefinitionNode>,
};

export type InputObjectTypeExtensionNode = {
  +kind: 'InputObjectTypeExtension',
  +loc?: Location,
  +name: NameNode,
  +directives?: $ReadOnlyArray<DirectiveNode>,
  +fields?: $ReadOnlyArray<InputValueDefinitionNode>,
};

// Directive Definitions

export type DirectiveDefinitionNode = {
  +kind: 'DirectiveDefinition',
  +loc?: Location,
  +description?: StringValueNode,
  +name: NameNode,
  +arguments?: $ReadOnlyArray<InputValueDefinitionNode>,
  +locations: $ReadOnlyArray<NameNode>,
};
