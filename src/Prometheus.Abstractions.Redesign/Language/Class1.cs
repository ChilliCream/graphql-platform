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
        SelectionSet,
        Field,
        Argument,
        FragmentSpread,
        InlineFragment,
        FragmentDefinition,
        IntValue,
        StringValue,
        BooleanValue,
        NullValue,
        EnumValue,
        ListValue,
        ObjectValue,
        ObjectField,
        Directive,
        NamedType
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
        public ITypeNode Type { get; }
        public IValueNode DefaultValue { get; }
    }

    public class DirectiveNode
    {
        public NodeKind Kind { get; } = NodeKind.Directive;
        public Location Location { get; }
        public NameNode Name { get; }
        public IReadOnlyCollection<ArgumentNode> Arguments { get; }
    }

    public class SelectionSetNode
        : ISyntaxNode
    {
        public NodeKind Kind { get; } = NodeKind.SelectionSet;
        public Location Location { get; }
        public IReadOnlyCollection<ISelectionNode> Selections { get; }
    }

    public class VariableNode
        : IValueNode
    {
        public NodeKind Kind { get; } = NodeKind.Variable;
        public Location Location { get; }
        public NameNode Name { get; }
    }

    public interface ITypeNode
      : ISyntaxNode
    {

    }

    public interface IValueNode
      : ISyntaxNode
    {

    }

    public interface ISelectionNode
        : ISyntaxNode
    {

    }

    public class FieldNode
      : ISelectionNode
    {
        public NodeKind Kind { get; } = NodeKind.Field;
        public Location Location { get; }
        public NameNode Name { get; }
        public NameNode Alias { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public IReadOnlyCollection<ArgumentNode> Arguments { get; }
        public SelectionSetNode SelectionSet { get; }
    }

    public class ArgumentNode
      : ISyntaxNode
    {
        public NodeKind Kind { get; } = NodeKind.Argument;
        public Location Location { get; }
        public NameNode Name { get; }
        public IValueNode Value { get; }
    }

    public class FragmentSpreadNode
      : ISelectionNode
    {
        public NodeKind Kind { get; } = NodeKind.FragmentSpread;
        public Location Location { get; }
        public NameNode Name { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
    }
    public class InlineFragmentNode
         : ISelectionNode
    {
        public NodeKind Kind { get; } = NodeKind.InlineFragment;
        public Location Location { get; }
        public NamedTypeNode TypeCondition { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public SelectionSetNode SelectionSet { get; }
    }

    public class NamedTypeNode
      : ITypeNode
    {
        public NodeKind Kind { get; } = NodeKind.NamedType;
        public Location Location { get; }
        public NameNode Name { get; }
    }

    public class FragmentDefinitionNode
      : IExecutableDefinitionNode
    {
        public NodeKind Kind { get; } = NodeKind.FragmentDefinition;
        public Location Location { get; }
        public NameNode Name { get; }
        public NamedTypeNode TypeCondition { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public SelectionSetNode SelectionSet { get; }
    }

    public class IntValueNode
      : IValueNode
    {
        public NodeKind Kind { get; } = NodeKind.IntValue;
        public Location Location { get; }
        public string Value { get; }
    }

    public class FloatValueNode
      : IValueNode
    {
        public NodeKind Kind { get; } = NodeKind.IntValue;
        public Location Location { get; }
        public string Value { get; }
    }

    public class StringValueNode
      : IValueNode
    {
        public NodeKind Kind { get; } = NodeKind.StringValue;
        public Location Location { get; }
        public string Value { get; }
        public bool? Block { get; }
    }

    public class BooleanValueNode
      : IValueNode
    {
        public NodeKind Kind { get; } = NodeKind.BooleanValue;
        public Location Location { get; }
        public bool Value { get; }
    }

    public class NullValueNode
      : IValueNode
    {
        public NodeKind Kind { get; } = NodeKind.NullValue;
        public Location Location { get; }
    }

    public class EnumValueNode
      : IValueNode
    {
        public NodeKind Kind { get; } = NodeKind.EnumValue;
        public Location Location { get; }
        public string Value { get; }
    }

    public class ListValueNode
      : IValueNode
    {
        public NodeKind Kind { get; } = NodeKind.ListValue;
        public Location Location { get; }
        public IReadOnlyCollection<IValueNode> Value { get; }
    }

    public class ObjectValueNode
      : IValueNode
    {
        public NodeKind Kind { get; } = NodeKind.ObjectValue;
        public Location Location { get; }
        public IReadOnlyCollection<ObjectFieldNode> Fields { get; }
    }

    public class ObjectFieldNode
      : ISyntaxNode
    {
        public NodeKind Kind { get; } = NodeKind.ObjectField;
        public Location Location { get; }
        public NameNode Name { get; }
        public IValueNode Value { get; }
    }


    export type ITypeNode = NamedTypeNode | ListTypeNode | NonNullTypeNode;



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
