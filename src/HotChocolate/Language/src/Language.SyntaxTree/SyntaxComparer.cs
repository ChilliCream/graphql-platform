using System;
using System.Collections.Generic;
using System.Linq;
using static HotChocolate.Language.SyntaxComparer;

namespace HotChocolate.Language;

public static class SyntaxComparer
{
    public static IEqualityComparer<ISyntaxNode> BySyntax { get; }
        = new SyntaxEqualityComparer();

    public static IEqualityComparer<ISyntaxNode> ByReference { get; }
        = new DefaultSyntaxEqualityComparer();

    private sealed class DefaultSyntaxEqualityComparer : IEqualityComparer<ISyntaxNode>
    {
        public bool Equals(ISyntaxNode? x, ISyntaxNode? y)
            => object.Equals(x, y);

        public int GetHashCode(ISyntaxNode obj)
            => obj.GetHashCode();
    }
}


internal sealed class SyntaxEqualityComparer : IEqualityComparer<ISyntaxNode>
{
    public bool Equals(ISyntaxNode? x, ISyntaxNode? y)
    {
        if (x is null)
        {
            return y is null;
        }

        if (y is null)
        {
            return false;
        }

        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x.GetType() != y.GetType())
        {
            return false;
        }

        switch (x.Kind)
        {
            case SyntaxKind.Name:
                return Equals((NameNode)x, (NameNode)y);

            case SyntaxKind.Document:
                return Equals((DocumentNode)x, (DocumentNode)y);

            case SyntaxKind.OperationDefinition:
                return Equals((OperationDefinitionNode)x, (OperationDefinitionNode)y);

            case SyntaxKind.VariableDefinition:
                break;

            case SyntaxKind.Variable:
                break;

            case SyntaxKind.SelectionSet:
                break;

            case SyntaxKind.Field:
                return Equals((FieldNode)x, (FieldNode)y);

            case SyntaxKind.Argument:
                return Equals((ArgumentNode)x, (ArgumentNode)y);

            case SyntaxKind.FragmentSpread:
                return Equals((FragmentSpreadNode)x, (FragmentSpreadNode)y);

            case SyntaxKind.InlineFragment:
                return Equals((InlineFragmentNode)x, (InlineFragmentNode)y);

            case SyntaxKind.FragmentDefinition:
                return Equals((FragmentDefinitionNode)x, (FragmentDefinitionNode)y);

            case SyntaxKind.IntValue:
                return Equals((IntValueNode)x, (IntValueNode)y);

            case SyntaxKind.StringValue:
                break;

            case SyntaxKind.BooleanValue:
                return Equals((BooleanValueNode)x, (BooleanValueNode)y);

            case SyntaxKind.NullValue:
                return true;

            case SyntaxKind.EnumValue:
                return Equals((EnumValueNode)x, (EnumValueNode)y);

            case SyntaxKind.ListValue:
                return Equals((ListValueNode)x, (ListValueNode)y);

            case SyntaxKind.ObjectValue:
                return Equals((ObjectValueNode)x, (ObjectValueNode)y);

            case SyntaxKind.ObjectField:
                return Equals((ObjectFieldNode)x, (ObjectFieldNode)y);

            case SyntaxKind.Directive:
                return Equals((DirectiveNode)x, (DirectiveNode)y);

            case SyntaxKind.NamedType:
                return Equals((NamedTypeNode)x, (NamedTypeNode)y);

            case SyntaxKind.ListType:
                return Equals((ListTypeNode)x, (ListTypeNode)y);

            case SyntaxKind.NonNullType:
                return Equals((NonNullTypeNode)x, (NonNullTypeNode)y);

            case SyntaxKind.SchemaDefinition:
                break;

            case SyntaxKind.OperationTypeDefinition:
                return Equals((OperationTypeDefinitionNode)x, (OperationTypeDefinitionNode)y);

            case SyntaxKind.ScalarTypeDefinition:
                return Equals((ScalarTypeDefinitionNode)x, (ScalarTypeDefinitionNode)y);

            case SyntaxKind.ObjectTypeDefinition:
                return Equals((ObjectTypeDefinitionNode)x, (ObjectTypeDefinitionNode)y);

            case SyntaxKind.FieldDefinition:
                return Equals((FieldDefinitionNode)x, (FieldDefinitionNode)y);

            case SyntaxKind.InputValueDefinition:
                return Equals((InputValueDefinitionNode)x, (InputValueDefinitionNode)y);

            case SyntaxKind.InterfaceTypeDefinition:
                return Equals((InterfaceTypeDefinitionNode)x, (InterfaceTypeDefinitionNode)y);

            case SyntaxKind.UnionTypeDefinition:
                break;

            case SyntaxKind.EnumTypeDefinition:
                return Equals((EnumTypeDefinitionNode)x, (EnumTypeDefinitionNode)y);

            case SyntaxKind.EnumValueDefinition:
                return Equals((EnumValueDefinitionNode)x, (EnumValueDefinitionNode)y);

            case SyntaxKind.InputObjectTypeDefinition:
                return Equals((InputObjectTypeDefinitionNode)x, (InputObjectTypeDefinitionNode)y);

            case SyntaxKind.SchemaExtension:
                break;

            case SyntaxKind.ScalarTypeExtension:
                return Equals((ScalarTypeExtensionNode)x, (ScalarTypeExtensionNode)y);

            case SyntaxKind.ObjectTypeExtension:
                return Equals((ObjectTypeExtensionNode)x, (ObjectTypeExtensionNode)y);

            case SyntaxKind.InterfaceTypeExtension:
                return Equals((InterfaceTypeExtensionNode)x, (InterfaceTypeExtensionNode)y);

            case SyntaxKind.UnionTypeExtension:
                break;

            case SyntaxKind.EnumTypeExtension:
                return Equals((EnumTypeExtensionNode)x, (EnumTypeExtensionNode)y);

            case SyntaxKind.InputObjectTypeExtension:
                return Equals((InputObjectTypeExtensionNode)x, (InputObjectTypeExtensionNode)y);

            case SyntaxKind.DirectiveDefinition:
                return Equals((DirectiveDefinitionNode)x, (DirectiveDefinitionNode)y);

            case SyntaxKind.FloatValue:
                return Equals((FloatValueNode)x, (FloatValueNode)y);

            case SyntaxKind.PublicKeyword:
                break;

            case SyntaxKind.ListNullability:
                return Equals((ListNullabilityNode)x, (ListNullabilityNode)y);

            case SyntaxKind.RequiredModifier:
                return Equals((RequiredModifierNode)x, (RequiredModifierNode)y);

            case SyntaxKind.OptionalModifier:
                return Equals((OptionalModifierNode)x, (OptionalModifierNode)y);

            case SyntaxKind.SchemaCoordinate:
                return Equals((SchemaCoordinateNode)x, (SchemaCoordinateNode)y);

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private bool Equals(ArgumentNode x, ArgumentNode y)
        => Equals(x.Name, y.Name) && Equals(x.Value, y.Value);

    private bool Equals(BooleanValueNode x, BooleanValueNode y)
        => x.Value.Equals(y.Value);

    private bool Equals(DirectiveDefinitionNode x, DirectiveDefinitionNode y)
        => Equals(x.Name, y.Name) &&
            Equals(x.Description, y.Description) &&
            x.IsRepeatable.Equals(y.IsRepeatable) &&
            Equals(x.Arguments, y.Arguments) &&
            Equals(x.Locations, y.Locations);

    private bool Equals(DirectiveNode x, DirectiveNode y)
        => Equals(x.Name, y.Name) && Equals(x.Arguments, y.Arguments);

    private bool Equals(DocumentNode x, DocumentNode y)
        => Equals(x.Definitions, y.Definitions);

    private bool Equals(EnumTypeDefinitionNode x, EnumTypeDefinitionNode y)
        => Equals(x.Name, y.Name) &&
            Equals(x.Description, y.Description) &&
            Equals(x.Directives, y.Directives) &&
            Equals(x.Values, y.Values);

    private bool Equals(EnumTypeExtensionNode x, EnumTypeExtensionNode y)
        => Equals(x.Name, y.Name) &&
            Equals(x.Directives, y.Directives) &&
            Equals(x.Values, y.Values);

    private bool Equals(EnumValueDefinitionNode x, EnumValueDefinitionNode y)
        => Equals(x.Name, y.Name) &&
            Equals(x.Description, y.Description);

    private bool Equals(EnumValueNode x, EnumValueNode y)
        => x.Value.Equals(y.Value, StringComparison.Ordinal);

    private bool Equals(FieldDefinitionNode x, FieldDefinitionNode y)
        => Equals(x.Name, y.Name) &&
            Equals(x.Description, y.Description) &&
            Equals(x.Directives, y.Directives) &&
            Equals(x.Arguments, y.Arguments) &&
            Equals(x.Type, y.Type);

    private bool Equals(FieldNode x, FieldNode y)
        => Equals(x.Name, y.Name) &&
            BySyntax.Equals(x.Alias, y.Alias) &&
            Equals(x.Arguments, y.Arguments) &&
            Equals(x.Directives, y.Directives) &&
            Equals(x.Required, y.Required) &&
            Equals(x.SelectionSet, y.SelectionSet);

    private bool Equals(FloatValueNode x, FloatValueNode y)
    {
        ReadOnlyMemory<byte> ourMem = x.AsMemory();
        ReadOnlyMemory<byte> otherMem = y.AsMemory();

        // memory is not doing a deep equality check,
        // but it will be equal if we are referring to the same
        // underlying array.
        if (otherMem.Equals(ourMem))
        {
            return true;
        }

        // if the length is not equals we can do a quick exit.
        if (ourMem.Length != otherMem.Length)
        {
            return false;
        }

        // last we will do a sequence equals and compare the utf8string representation of
        // this value.
        return ourMem.Span.SequenceEqual(otherMem.Span);
    }

    private bool Equals(FragmentDefinitionNode x, FragmentDefinitionNode y)
        => Equals(x.Name, y.Name) &&
            Equals(x.TypeCondition, y.TypeCondition) &&
            Equals(x.Directives, y.Directives) &&
            Equals(x.VariableDefinitions, y.VariableDefinitions) &&
            Equals(x.SelectionSet, y.SelectionSet);

    private bool Equals(FragmentSpreadNode x, FragmentSpreadNode y)
        => Equals(x.Name, y.Name) &&
            Equals(x.Directives, y.Directives);

    private bool Equals(InlineFragmentNode x, InlineFragmentNode y)
        => Equals(x.TypeCondition, y.TypeCondition) &&
            Equals(x.SelectionSet, y.SelectionSet) &&
            Equals(x.Directives, y.Directives);

    private bool Equals(InputObjectTypeDefinitionNode x, InputObjectTypeDefinitionNode y)
        => Equals(x.Name, y.Name) &&
            Equals(x.Description, y.Description) &&
            Equals(x.Directives, y.Directives) &&
            Equals(x.Fields, y.Fields);

    private bool Equals(InputObjectTypeExtensionNode x, InputObjectTypeExtensionNode y)
        => Equals(x.Name, y.Name) &&
            Equals(x.Directives, y.Directives) &&
            Equals(x.Fields, y.Fields);

    private bool Equals(InputValueDefinitionNode x, InputValueDefinitionNode y)
        => Equals(x.Name, y.Name) &&
            Equals(x.Description, y.Description) &&
            Equals(x.Type, y.Type) &&
            Equals(x.DefaultValue, y.DefaultValue) &&
            Equals(x.Directives, y.Directives);

    private bool Equals(InterfaceTypeDefinitionNode x, InterfaceTypeDefinitionNode y)
        => Equals(x.Name, y.Name) &&
            Equals(x.Description, y.Description) &&
            Equals(x.Directives, y.Directives) &&
            Equals(x.Interfaces, y.Interfaces) &&
            Equals(x.Fields, y.Fields);

    private bool Equals(InterfaceTypeExtensionNode x, InterfaceTypeExtensionNode y)
        => Equals(x.Name, y.Name) &&
            Equals(x.Directives, y.Directives) &&
            Equals(x.Interfaces, y.Interfaces) &&
            Equals(x.Fields, y.Fields);

    private bool Equals(IntValueNode x, IntValueNode y)
    {
        ReadOnlyMemory<byte> ourMem = x.AsMemory();
        ReadOnlyMemory<byte> otherMem = y.AsMemory();

        // memory is not doing a deep equality check,
        // but it will be equal if we are referring to the same
        // underlying array.
        if (otherMem.Equals(ourMem))
        {
            return true;
        }

        // if the length is not equals we can do a quick exit.
        if (ourMem.Length != otherMem.Length)
        {
            return false;
        }

        // last we will do a sequence equals and compare the utf8string representation of
        // this value.
        return ourMem.Span.SequenceEqual(otherMem.Span);
    }

    private bool Equals(ListNullabilityNode x, ListNullabilityNode y)
        => Equals(x.Element, y.Element);

    private bool Equals(ListTypeNode x, ListTypeNode y)
        => Equals(x.Type, y.Type);

    private bool Equals(ListValueNode x, ListValueNode y)
    {
        if (x.Items.Count == y.Items.Count)
        {
            for (var i = 0; i < x.Items.Count; i++)
            {
                if (!x.Items[i].Equals(y.Items[i]))
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    private bool Equals(NamedTypeNode x, NamedTypeNode y)
        => Equals(x.Name, y.Name);

    private bool Equals(NameNode x, NameNode y)
        => x.Value.Equals(y.Value, StringComparison.Ordinal);

    private bool Equals(NonNullTypeNode x, NonNullTypeNode y)
        => Equals(x.Type, y.Type);

    private bool Equals(ObjectFieldNode x, ObjectFieldNode y)
        => Equals(x.Name, y.Name) &&
            Equals(x.Value, y.Value);

    private bool Equals(ObjectTypeDefinitionNode x, ObjectTypeDefinitionNode y)
        => Equals(x.Name, y.Name) &&
            Equals(x.Description, y.Description) &&
            Equals(x.Directives, y.Directives) &&
            Equals(x.Interfaces, y.Interfaces) &&
            Equals(x.Fields, y.Fields);

    private bool Equals(ObjectTypeExtensionNode x, ObjectTypeExtensionNode y)
        => Equals(x.Name, y.Name) &&
            Equals(x.Directives, y.Directives) &&
            Equals(x.Interfaces, y.Interfaces) &&
            Equals(x.Fields, y.Fields);

    private bool Equals(ObjectValueNode x, ObjectValueNode y)
        => Equals(x.Fields, y.Fields);

    private bool Equals(OperationDefinitionNode x, OperationDefinitionNode y)
        => BySyntax.Equals(x.Name, y.Name) &&
            Equals(x.Operation, y.Operation) &&
            Equals(x.VariableDefinitions, y.VariableDefinitions) &&
            Equals(x.Directives, y.Directives) &&
            Equals(x.SelectionSet, y.SelectionSet);

    private bool Equals(OperationTypeDefinitionNode x, OperationTypeDefinitionNode y)
        => Equals(x.Operation, y.Operation) &&
            Equals(x.Type, y.Type);

    private bool Equals(OptionalModifierNode x, OptionalModifierNode y)
        => BySyntax.Equals(x.Element, y.Element);

    private bool Equals(RequiredModifierNode x, RequiredModifierNode y)
        => BySyntax.Equals(x.Element, y.Element);

    private bool Equals(ScalarTypeDefinitionNode x, ScalarTypeDefinitionNode y)
        => Equals(x.Name, y.Name) &&
            Equals(x.Description, y.Description) &&
            Equals(x.Directives, y.Directives);

    private bool Equals(ScalarTypeExtensionNode x, ScalarTypeExtensionNode y)
        => Equals(x.Name, y.Name) &&
            Equals(x.Directives, y.Directives);

    private bool Equals(SchemaCoordinateNode x, SchemaCoordinateNode y)
        => Equals(x.OfDirective, y.OfDirective) &&
            Equals(x.Name, y.Name) &&
            BySyntax.Equals(x.MemberName, y.MemberName) &&
            BySyntax.Equals(x.ArgumentName, y.ArgumentName);

    private bool Equals(SchemaDefinitionNode x, SchemaDefinitionNode y)
        => BySyntax.Equals(x.Description, y.Description) &&
            Equals(x.Directives, y.Directives) &&
            Equals(x.OperationTypes, y.OperationTypes);

    private bool Equals(SchemaExtensionNode x, SchemaExtensionNode y)
        => Equals(x.Directives, y.Directives) &&
            Equals(x.OperationTypes, y.OperationTypes);

    private bool Equals(IReadOnlyList<ISyntaxNode> a, IReadOnlyList<ISyntaxNode> b)
    {
        if (a.Count == 0 && b.Count == 0)
        {
            return true;
        }

        return a.SequenceEqual(b, this);
    }

    public int GetHashCode(ISyntaxNode obj)
    {
        switch (obj.Kind)
        {
            case SyntaxKind.Name:
                return GetHashCode((NameNode)obj);

            case SyntaxKind.Document:
                return GetHashCode((DocumentNode)obj);

            case SyntaxKind.OperationDefinition:
                return GetHashCode((OperationDefinitionNode)obj);

            case SyntaxKind.VariableDefinition:
                break;

            case SyntaxKind.Variable:
                break;

            case SyntaxKind.SelectionSet:
                break;

            case SyntaxKind.Field:
                return GetHashCode((FieldNode)obj);

            case SyntaxKind.Argument:
                return GetHashCode((ArgumentNode)obj);

            case SyntaxKind.FragmentSpread:
                return GetHashCode((FragmentSpreadNode)obj);

            case SyntaxKind.InlineFragment:
                return GetHashCode((InlineFragmentNode)obj);

            case SyntaxKind.FragmentDefinition:
                return GetHashCode((FragmentDefinitionNode)obj);

            case SyntaxKind.IntValue:
                return GetHashCode((IntValueNode)obj);

            case SyntaxKind.StringValue:
                break;

            case SyntaxKind.BooleanValue:
                return GetHashCode((BooleanValueNode)obj);

            case SyntaxKind.NullValue:
                return GetHashCode((NullValueNode)obj);

            case SyntaxKind.EnumValue:
                return GetHashCode((EnumValueNode)obj);

            case SyntaxKind.ListValue:
                return GetHashCode((ListValueNode)obj);

            case SyntaxKind.ObjectValue:
                return GetHashCode((ObjectValueNode)obj);

            case SyntaxKind.ObjectField:
                return GetHashCode((ObjectFieldNode)obj);

            case SyntaxKind.Directive:
                return GetHashCode((DirectiveNode)obj);

            case SyntaxKind.NamedType:
                return GetHashCode((NamedTypeNode)obj);

            case SyntaxKind.ListType:
                return GetHashCode((ListTypeNode)obj);

            case SyntaxKind.NonNullType:
                return GetHashCode((NonNullTypeNode)obj);

            case SyntaxKind.SchemaDefinition:
                return GetHashCode((SchemaDefinitionNode)obj);

            case SyntaxKind.OperationTypeDefinition:
                return GetHashCode((OperationTypeDefinitionNode)obj);

            case SyntaxKind.ScalarTypeDefinition:
                return GetHashCode((ScalarTypeDefinitionNode)obj);

            case SyntaxKind.ObjectTypeDefinition:
                return GetHashCode((ObjectTypeDefinitionNode)obj);

            case SyntaxKind.FieldDefinition:
                return GetHashCode((FieldDefinitionNode)obj);

            case SyntaxKind.InputValueDefinition:
                return GetHashCode((InputValueDefinitionNode)obj);

            case SyntaxKind.InterfaceTypeDefinition:
                return GetHashCode((InterfaceTypeDefinitionNode)obj);

            case SyntaxKind.UnionTypeDefinition:
                break;

            case SyntaxKind.EnumTypeDefinition:
                return GetHashCode((EnumTypeDefinitionNode)obj);

            case SyntaxKind.EnumValueDefinition:
                return GetHashCode((EnumValueDefinitionNode)obj);

            case SyntaxKind.InputObjectTypeDefinition:
                return GetHashCode((InputObjectTypeDefinitionNode)obj);

            case SyntaxKind.SchemaExtension:
                return GetHashCode((SchemaExtensionNode)obj);

            case SyntaxKind.ScalarTypeExtension:
                return GetHashCode((ScalarTypeExtensionNode)obj);

            case SyntaxKind.ObjectTypeExtension:
                return GetHashCode((ObjectTypeExtensionNode)obj);

            case SyntaxKind.InterfaceTypeExtension:
                return GetHashCode((InterfaceTypeExtensionNode)obj);

            case SyntaxKind.UnionTypeExtension:
                break;

            case SyntaxKind.EnumTypeExtension:
                return GetHashCode((EnumTypeExtensionNode)obj);

            case SyntaxKind.InputObjectTypeExtension:
                return GetHashCode((InputObjectTypeExtensionNode)obj);

            case SyntaxKind.DirectiveDefinition:
                return GetHashCode((DirectiveDefinitionNode)obj);

            case SyntaxKind.FloatValue:
                return GetHashCode((FloatValueNode)obj);

            case SyntaxKind.PublicKeyword:
                break;

            case SyntaxKind.ListNullability:
                return GetHashCode((ListNullabilityNode)obj);

            case SyntaxKind.RequiredModifier:
                return GetHashCode((RequiredModifierNode)obj);

            case SyntaxKind.OptionalModifier:
                return GetHashCode((OptionalModifierNode)obj);

            case SyntaxKind.SchemaCoordinate:
                return GetHashCode((SchemaCoordinateNode)obj);

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private int GetHashCode(ArgumentNode node)
        => HashCode.Combine(node.Kind, GetHashCode(node.Name), GetHashCode(node.Value));

    private int GetHashCode(BooleanValueNode node)
        => HashCode.Combine(node.Kind, node.Value);

    private int GetHashCode(DirectiveDefinitionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));
        hashCode.Add(GetHashCode(node.Description));
        hashCode.Add(node.IsRepeatable);

        foreach (InputValueDefinitionNode argument in node.Arguments)
        {
            hashCode.Add(GetHashCode(argument));
        }

        foreach (NameNode location in node.Locations)
        {
            hashCode.Add(GetHashCode(location));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(DirectiveNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));

        foreach (ArgumentNode argument in node.Arguments)
        {
            hashCode.Add(GetHashCode(argument));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(DocumentNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);

        foreach (IDefinitionNode definition in node.Definitions)
        {
            hashCode.Add(GetHashCode(definition));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(EnumTypeDefinitionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));
        hashCode.Add(GetHashCode(node.Description));

        foreach (DirectiveNode directive in node.Directives)
        {
            hashCode.Add(GetHashCode(directive));
        }

        foreach (EnumValueDefinitionNode value in node.Values)
        {
            hashCode.Add(GetHashCode(value));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(EnumTypeExtensionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));

        foreach (DirectiveNode directive in node.Directives)
        {
            hashCode.Add(GetHashCode(directive));
        }

        foreach (EnumValueDefinitionNode value in node.Values)
        {
            hashCode.Add(GetHashCode(value));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(EnumValueDefinitionNode node)
        => HashCode.Combine(node.Kind, GetHashCode(node.Name), GetHashCode(node.Description));

    private int GetHashCode(EnumValueNode node)
        => HashCode.Combine(node.Kind, GetHashCode(node.Value));

    private int GetHashCode(FieldDefinitionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));
        hashCode.Add(GetHashCode(node.Description));

        foreach (DirectiveNode directive in node.Directives)
        {
            hashCode.Add(GetHashCode(directive));
        }

        foreach (InputValueDefinitionNode argument in node.Arguments)
        {
            hashCode.Add(GetHashCode(argument));
        }

        hashCode.Add(GetHashCode(node.Type));

        return hashCode.ToHashCode();
    }

    private int GetHashCode(FieldNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));
        hashCode.Add(GetHashCode(node.Alias));

        foreach (ArgumentNode argument in node.Arguments)
        {
            hashCode.Add(GetHashCode(argument));
        }

        foreach (DirectiveNode directive in node.Directives)
        {
            hashCode.Add(GetHashCode(directive));
        }

        hashCode.Add(GetHashCode(node.Required));
        hashCode.Add(GetHashCode(node.SelectionSet));

        return hashCode.ToHashCode();
    }

    private int GetHashCode(FloatValueNode node)
    {
#if NET6_0_OR_GREATER
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.AddBytes(node.AsSpan());
        return hashCode.ToHashCode();
#else
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        HashCodeExtensions.AddBytes(ref hashCode, node.AsSpan());
        return hashCode.ToHashCode();
#endif
    }

    private int GetHashCode(FragmentDefinitionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));
        hashCode.Add(GetHashCode(node.TypeCondition));

        foreach (VariableDefinitionNode variableDefinition in node.VariableDefinitions)
        {
            hashCode.Add(GetHashCode(variableDefinition));
        }

        foreach (DirectiveNode directive in node.Directives)
        {
            hashCode.Add(GetHashCode(directive));
        }

        hashCode.Add(GetHashCode(node.SelectionSet));

        return hashCode.ToHashCode();
    }

    private int GetHashCode(FragmentSpreadNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));

        foreach (DirectiveNode directive in node.Directives)
        {
            hashCode.Add(GetHashCode(directive));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(InlineFragmentNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.TypeCondition));

        foreach (DirectiveNode directive in node.Directives)
        {
            hashCode.Add(GetHashCode(directive));
        }

        hashCode.Add(GetHashCode(node.SelectionSet));

        return hashCode.ToHashCode();
    }

    private int GetHashCode(InputObjectTypeDefinitionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));
        hashCode.Add(GetHashCode(node.Description));

        foreach (DirectiveNode directive in node.Directives)
        {
            hashCode.Add(GetHashCode(directive));
        }

        foreach (InputValueDefinitionNode field in node.Fields)
        {
            hashCode.Add(GetHashCode(field));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(InputObjectTypeExtensionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));

        foreach (DirectiveNode directive in node.Directives)
        {
            hashCode.Add(GetHashCode(directive));
        }

        foreach (InputValueDefinitionNode field in node.Fields)
        {
            hashCode.Add(GetHashCode(field));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(InputValueDefinitionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));
        hashCode.Add(GetHashCode(node.Description));
        hashCode.Add(GetHashCode(node.Type));
        hashCode.Add(GetHashCode(node.DefaultValue));

        foreach (DirectiveNode directive in node.Directives)
        {
            hashCode.Add(GetHashCode(directive));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(InterfaceTypeDefinitionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));
        hashCode.Add(GetHashCode(node.Description));

        foreach (DirectiveNode directive in node.Directives)
        {
            hashCode.Add(GetHashCode(directive));
        }

        foreach (NamedTypeNode interfaceName in node.Interfaces)
        {
            hashCode.Add(GetHashCode(interfaceName));
        }

        foreach (FieldDefinitionNode field in node.Fields)
        {
            hashCode.Add(GetHashCode(field));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(InterfaceTypeExtensionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));

        foreach (DirectiveNode directive in node.Directives)
        {
            hashCode.Add(GetHashCode(directive));
        }

        foreach (NamedTypeNode interfaceName in node.Interfaces)
        {
            hashCode.Add(GetHashCode(interfaceName));
        }

        foreach (FieldDefinitionNode field in node.Fields)
        {
            hashCode.Add(GetHashCode(field));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(IntValueNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
#if NET6_0_OR_GREATER
        hashCode.AddBytes(node.AsSpan());
#else
        HashCodeExtensions.AddBytes(ref hashCode, node.AsSpan());
#endif
        return hashCode.ToHashCode();
    }

    private int GetHashCode(INullabilityNode? node)
    {
        if (node is null)
        {
            return 0;
        }

        return node.Kind switch
        {
            SyntaxKind.ListNullability => GetHashCode((ListNullabilityNode)node),
            SyntaxKind.RequiredModifier => GetHashCode((RequiredModifierNode)node),
            SyntaxKind.OptionalModifier => GetHashCode((OptionalModifierNode)node),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private int GetHashCode(ListNullabilityNode? node)
        => node is null ? 0 : HashCode.Combine(node.Kind, GetHashCode(node.Element));

    private int GetHashCode(ListTypeNode node)
        => HashCode.Combine(node.Kind, GetHashCode(node.Type));

    private int GetHashCode(ListValueNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);

        foreach (IValueNode item in node.Items)
        {
            hashCode.Add(GetHashCode(item));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(NamedTypeNode node)
        => HashCode.Combine(node.Kind, GetHashCode(node.Name));

    private int GetHashCode(NameNode? node)
        => node is null ? 0 : HashCode.Combine(node.Kind, node.Value);

    private int GetHashCode(NonNullTypeNode node)
        => HashCode.Combine(node.Kind, GetHashCode(node.Type));

    private int GetHashCode(NullValueNode node)
        => HashCode.Combine(node.Kind);

    private int GetHashCode(ObjectFieldNode node)
        => HashCode.Combine(node.Kind, GetHashCode(node.Name), GetHashCode(node.Value));

    private int GetHashCode(ObjectTypeDefinitionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));
        hashCode.Add(GetHashCode(node.Description));

        foreach (DirectiveNode directive in node.Directives)
        {
            hashCode.Add(GetHashCode(directive));
        }

        foreach (NamedTypeNode interfaceName in node.Interfaces)
        {
            hashCode.Add(GetHashCode(interfaceName));
        }

        foreach (FieldDefinitionNode field in node.Fields)
        {
            hashCode.Add(GetHashCode(field));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(ObjectTypeExtensionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));

        foreach (DirectiveNode directive in node.Directives)
        {
            hashCode.Add(GetHashCode(directive));
        }

        foreach (NamedTypeNode interfaceName in node.Interfaces)
        {
            hashCode.Add(GetHashCode(interfaceName));
        }

        foreach (FieldDefinitionNode field in node.Fields)
        {
            hashCode.Add(GetHashCode(field));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(ObjectValueNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);

        foreach (ObjectFieldNode field in node.Fields)
        {
            hashCode.Add(GetHashCode(field));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(OperationDefinitionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));
        hashCode.Add(node.Operation);

        foreach (VariableDefinitionNode variableDefinition in node.VariableDefinitions)
        {
            hashCode.Add(GetHashCode(variableDefinition));
        }

        foreach (DirectiveNode directive in node.Directives)
        {
            hashCode.Add(GetHashCode(directive));
        }

        hashCode.Add(GetHashCode(node.SelectionSet));

        return hashCode.ToHashCode();
    }

    private int GetHashCode(OperationTypeDefinitionNode node)
        => HashCode.Combine(node.Kind, node.Operation, GetHashCode(node.Type));

    private int GetHashCode(OptionalModifierNode? node)
        => node is null ? 0 : HashCode.Combine(node.Kind, GetHashCode(node.Element));

    private int GetHashCode(RequiredModifierNode? node)
        => node is null ? 0 : HashCode.Combine(node.Kind, GetHashCode(node.Element));

    private int GetHashCode(ScalarTypeDefinitionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));
        hashCode.Add(GetHashCode(node.Description));

        foreach (DirectiveNode directive in node.Directives)
        {
            hashCode.Add(GetHashCode(directive));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(ScalarTypeExtensionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));

        foreach (DirectiveNode directive in node.Directives)
        {
            hashCode.Add(GetHashCode(directive));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(SchemaCoordinateNode node)
        => HashCode.Combine(
            node.Kind,
            node.OfDirective,
            GetHashCode(node.Name),
            GetHashCode(node.MemberName),
            GetHashCode(node.ArgumentName));

    private int GetHashCode(SchemaDefinitionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Description));

        foreach (DirectiveNode directive in node.Directives)
        {
            hashCode.Add(GetHashCode(directive));
        }

        foreach (OperationTypeDefinitionNode operationType in node.OperationTypes)
        {
            hashCode.Add(GetHashCode(operationType));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(SchemaExtensionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);

        foreach (DirectiveNode directive in node.Directives)
        {
            hashCode.Add(GetHashCode(directive));
        }

        foreach (OperationTypeDefinitionNode operationType in node.OperationTypes)
        {
            hashCode.Add(GetHashCode(operationType));
        }

        return hashCode.ToHashCode();
    }
}

