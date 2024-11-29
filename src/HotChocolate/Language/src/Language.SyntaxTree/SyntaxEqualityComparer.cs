namespace HotChocolate.Language;

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
                return Equals((VariableDefinitionNode)x, (VariableDefinitionNode)y);

            case SyntaxKind.Variable:
                return Equals((VariableNode)x, (VariableNode)y);

            case SyntaxKind.SelectionSet:
                return Equals((SelectionSetNode)x, (SelectionSetNode)y);

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
                return Equals((StringValueNode)x, (StringValueNode)y);

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
                return Equals((SchemaDefinitionNode)x, (SchemaDefinitionNode)y);

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
                return Equals((UnionTypeDefinitionNode)x, (UnionTypeDefinitionNode)y);

            case SyntaxKind.EnumTypeDefinition:
                return Equals((EnumTypeDefinitionNode)x, (EnumTypeDefinitionNode)y);

            case SyntaxKind.EnumValueDefinition:
                return Equals((EnumValueDefinitionNode)x, (EnumValueDefinitionNode)y);

            case SyntaxKind.InputObjectTypeDefinition:
                return Equals((InputObjectTypeDefinitionNode)x, (InputObjectTypeDefinitionNode)y);

            case SyntaxKind.SchemaExtension:
                return Equals((SchemaExtensionNode)x, (SchemaExtensionNode)y);

            case SyntaxKind.ScalarTypeExtension:
                return Equals((ScalarTypeExtensionNode)x, (ScalarTypeExtensionNode)y);

            case SyntaxKind.ObjectTypeExtension:
                return Equals((ObjectTypeExtensionNode)x, (ObjectTypeExtensionNode)y);

            case SyntaxKind.InterfaceTypeExtension:
                return Equals((InterfaceTypeExtensionNode)x, (InterfaceTypeExtensionNode)y);

            case SyntaxKind.UnionTypeExtension:
                return Equals((UnionTypeExtensionNode)x, (UnionTypeExtensionNode)y);

            case SyntaxKind.EnumTypeExtension:
                return Equals((EnumTypeExtensionNode)x, (EnumTypeExtensionNode)y);

            case SyntaxKind.InputObjectTypeExtension:
                return Equals((InputObjectTypeExtensionNode)x, (InputObjectTypeExtensionNode)y);

            case SyntaxKind.DirectiveDefinition:
                return Equals((DirectiveDefinitionNode)x, (DirectiveDefinitionNode)y);

            case SyntaxKind.FloatValue:
                return Equals((FloatValueNode)x, (FloatValueNode)y);

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
            SyntaxComparer.BySyntax.Equals(x.Description, y.Description) &&
            x.IsRepeatable.Equals(y.IsRepeatable) &&
            Equals(x.Arguments, y.Arguments) &&
            Equals(x.Locations, y.Locations);

    private bool Equals(DirectiveNode x, DirectiveNode y)
        => Equals(x.Name, y.Name) && Equals(x.Arguments, y.Arguments);

    private bool Equals(DocumentNode x, DocumentNode y)
        => Equals(x.Definitions, y.Definitions);

    private bool Equals(EnumTypeDefinitionNode x, EnumTypeDefinitionNode y)
        => Equals(x.Name, y.Name) &&
            SyntaxComparer.BySyntax.Equals(x.Description, y.Description) &&
            Equals(x.Directives, y.Directives) &&
            Equals(x.Values, y.Values);

    private bool Equals(EnumTypeExtensionNode x, EnumTypeExtensionNode y)
        => Equals(x.Name, y.Name) &&
            Equals(x.Directives, y.Directives) &&
            Equals(x.Values, y.Values);

    private bool Equals(EnumValueDefinitionNode x, EnumValueDefinitionNode y)
        => Equals(x.Name, y.Name) &&
            SyntaxComparer.BySyntax.Equals(x.Description, y.Description);

    private bool Equals(EnumValueNode x, EnumValueNode y)
        => x.Value.Equals(y.Value, StringComparison.Ordinal);

    private bool Equals(FieldDefinitionNode x, FieldDefinitionNode y)
        => Equals(x.Name, y.Name) &&
            SyntaxComparer.BySyntax.Equals(x.Description, y.Description) &&
            Equals(x.Directives, y.Directives) &&
            Equals(x.Arguments, y.Arguments) &&
            Equals(x.Type, y.Type);

    private bool Equals(FieldNode x, FieldNode y)
        => Equals(x.Name, y.Name) &&
            SyntaxComparer.BySyntax.Equals(x.Alias, y.Alias) &&
            Equals(x.Arguments, y.Arguments) &&
            Equals(x.Directives, y.Directives) &&
            SyntaxComparer.BySyntax.Equals(x.SelectionSet, y.SelectionSet);

    private bool Equals(FloatValueNode x, FloatValueNode y)
    {
        var ourMem = x.AsMemory();
        var otherMem = y.AsMemory();

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
        => SyntaxComparer.BySyntax.Equals(x.TypeCondition, y.TypeCondition) &&
            Equals(x.SelectionSet, y.SelectionSet) &&
            Equals(x.Directives, y.Directives);

    private bool Equals(InputObjectTypeDefinitionNode x, InputObjectTypeDefinitionNode y)
        => Equals(x.Name, y.Name) &&
            SyntaxComparer.BySyntax.Equals(x.Description, y.Description) &&
            Equals(x.Directives, y.Directives) &&
            Equals(x.Fields, y.Fields);

    private bool Equals(InputObjectTypeExtensionNode x, InputObjectTypeExtensionNode y)
        => Equals(x.Name, y.Name) &&
            Equals(x.Directives, y.Directives) &&
            Equals(x.Fields, y.Fields);

    private bool Equals(InputValueDefinitionNode x, InputValueDefinitionNode y)
        => Equals(x.Name, y.Name) &&
            SyntaxComparer.BySyntax.Equals(x.Description, y.Description) &&
            Equals(x.Type, y.Type) &&
            Equals(x.DefaultValue, y.DefaultValue) &&
            Equals(x.Directives, y.Directives);

    private bool Equals(InterfaceTypeDefinitionNode x, InterfaceTypeDefinitionNode y)
        => Equals(x.Name, y.Name) &&
            SyntaxComparer.BySyntax.Equals(x.Description, y.Description) &&
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
        var ourMem = x.AsMemory();
        var otherMem = y.AsMemory();

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

    private bool Equals(ListTypeNode x, ListTypeNode y)
        => Equals(x.Type, y.Type);

    private bool Equals(ListValueNode x, ListValueNode y)
    {
        if (x.Items.Count == y.Items.Count)
        {
            for (var i = 0; i < x.Items.Count; i++)
            {
                if (!Equals(x.Items[i], y.Items[i]))
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
            SyntaxComparer.BySyntax.Equals(x.Description, y.Description) &&
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
        => SyntaxComparer.BySyntax.Equals(x.Name, y.Name) &&
            Equals(x.Operation, y.Operation) &&
            Equals(x.VariableDefinitions, y.VariableDefinitions) &&
            Equals(x.Directives, y.Directives) &&
            Equals(x.SelectionSet, y.SelectionSet);

    private bool Equals(OperationTypeDefinitionNode x, OperationTypeDefinitionNode y)
        => Equals(x.Operation, y.Operation) &&
            Equals(x.Type, y.Type);

    private bool Equals(ScalarTypeDefinitionNode x, ScalarTypeDefinitionNode y)
        => Equals(x.Name, y.Name) &&
            SyntaxComparer.BySyntax.Equals(x.Description, y.Description) &&
            Equals(x.Directives, y.Directives);

    private bool Equals(ScalarTypeExtensionNode x, ScalarTypeExtensionNode y)
        => Equals(x.Name, y.Name) &&
            Equals(x.Directives, y.Directives);

    private bool Equals(SchemaCoordinateNode x, SchemaCoordinateNode y)
        => Equals(x.OfDirective, y.OfDirective) &&
            Equals(x.Name, y.Name) &&
            SyntaxComparer.BySyntax.Equals(x.MemberName, y.MemberName) &&
            SyntaxComparer.BySyntax.Equals(x.ArgumentName, y.ArgumentName);

    private bool Equals(SchemaDefinitionNode x, SchemaDefinitionNode y)
        => SyntaxComparer.BySyntax.Equals(x.Description, y.Description) &&
            Equals(x.Directives, y.Directives) &&
            Equals(x.OperationTypes, y.OperationTypes);

    private bool Equals(SchemaExtensionNode x, SchemaExtensionNode y)
        => Equals(x.Directives, y.Directives) &&
            Equals(x.OperationTypes, y.OperationTypes);

    private bool Equals(SelectionSetNode x, SelectionSetNode y)
        => Equals(x.Selections, y.Selections);

    private bool Equals(StringValueNode x, StringValueNode y)
    {
        var ourMem = x.AsMemory();
        var otherMem = y.AsMemory();

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

    private bool Equals(UnionTypeDefinitionNode x, UnionTypeDefinitionNode y)
        => Equals(x.Name, y.Name) &&
            SyntaxComparer.BySyntax.Equals(x.Description, y.Description) &&
            Equals(x.Directives, y.Directives) &&
            Equals(x.Types, y.Types);

    private bool Equals(UnionTypeExtensionNode x, UnionTypeExtensionNode y)
        => Equals(x.Name, y.Name) &&
            Equals(x.Directives, y.Directives) &&
            Equals(x.Types, y.Types);

    private bool Equals(VariableDefinitionNode x, VariableDefinitionNode y)
        => Equals(x.Variable, y.Variable) &&
            Equals(x.Type, y.Type) &&
            Equals(x.DefaultValue, y.DefaultValue) &&
            Equals(x.Directives, y.Directives);

    private bool Equals(VariableNode x, VariableNode y)
        => Equals(x.Name, y.Name);

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
                return GetHashCode((VariableDefinitionNode)obj);

            case SyntaxKind.Variable:
                return GetHashCode((VariableNode)obj);

            case SyntaxKind.SelectionSet:
                return GetHashCode((SelectionSetNode)obj);

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
                return GetHashCode((StringValueNode)obj);

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
                return GetHashCode((UnionTypeDefinitionNode)obj);

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
                return GetHashCode((UnionTypeExtensionNode)obj);

            case SyntaxKind.EnumTypeExtension:
                return GetHashCode((EnumTypeExtensionNode)obj);

            case SyntaxKind.InputObjectTypeExtension:
                return GetHashCode((InputObjectTypeExtensionNode)obj);

            case SyntaxKind.DirectiveDefinition:
                return GetHashCode((DirectiveDefinitionNode)obj);

            case SyntaxKind.FloatValue:
                return GetHashCode((FloatValueNode)obj);

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

        for (var i = 0; i < node.Arguments.Count; i++)
        {
            var argument = node.Arguments[i];
            hashCode.Add(GetHashCode(argument));
        }

        for (var i = 0; i < node.Locations.Count; i++)
        {
            var location = node.Locations[i];
            hashCode.Add(GetHashCode(location));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(DirectiveNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));

        for (var i = 0; i < node.Arguments.Count; i++)
        {
            var argument = node.Arguments[i];
            hashCode.Add(GetHashCode(argument));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(DocumentNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);

        for (var i = 0; i < node.Definitions.Count; i++)
        {
            var definition = node.Definitions[i];
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

        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];
            hashCode.Add(GetHashCode(directive));
        }

        for (var i = 0; i < node.Values.Count; i++)
        {
            var value = node.Values[i];
            hashCode.Add(GetHashCode(value));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(EnumTypeExtensionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));

        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];
            hashCode.Add(GetHashCode(directive));
        }

        for (var i = 0; i < node.Values.Count; i++)
        {
            var value = node.Values[i];
            hashCode.Add(GetHashCode(value));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(EnumValueDefinitionNode node)
        => HashCode.Combine(node.Kind, GetHashCode(node.Name), GetHashCode(node.Description));

    private int GetHashCode(EnumValueNode node)
        => HashCode.Combine(node.Kind, node.Value);

    private int GetHashCode(FieldDefinitionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));
        hashCode.Add(GetHashCode(node.Description));

        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];
            hashCode.Add(GetHashCode(directive));
        }

        for (var i = 0; i < node.Arguments.Count; i++)
        {
            var argument = node.Arguments[i];
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

        for (var i = 0; i < node.Arguments.Count; i++)
        {
            var argument = node.Arguments[i];
            hashCode.Add(GetHashCode(argument));
        }

        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];
            hashCode.Add(GetHashCode(directive));
        }

        hashCode.Add(GetHashCode(node.SelectionSet));

        return hashCode.ToHashCode();
    }

    private int GetHashCode(FloatValueNode node)
    {
#if NETSTANDARD2_0
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        HashCodeExtensions.AddBytes(ref hashCode, node.AsSpan());
        return hashCode.ToHashCode();
#else
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.AddBytes(node.AsSpan());
        return hashCode.ToHashCode();
#endif
    }

    private int GetHashCode(FragmentDefinitionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));
        hashCode.Add(GetHashCode(node.TypeCondition));

        for (var i = 0; i < node.VariableDefinitions.Count; i++)
        {
            var variableDefinition = node.VariableDefinitions[i];
            hashCode.Add(GetHashCode(variableDefinition));
        }

        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];
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

        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];
            hashCode.Add(GetHashCode(directive));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(InlineFragmentNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.TypeCondition));

        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];
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

        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];
            hashCode.Add(GetHashCode(directive));
        }

        for (var i = 0; i < node.Fields.Count; i++)
        {
            var field = node.Fields[i];
            hashCode.Add(GetHashCode(field));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(InputObjectTypeExtensionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));

        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];
            hashCode.Add(GetHashCode(directive));
        }

        for (var i = 0; i < node.Fields.Count; i++)
        {
            var field = node.Fields[i];
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

        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];
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

        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];
            hashCode.Add(GetHashCode(directive));
        }

        for (var i = 0; i < node.Interfaces.Count; i++)
        {
            var interfaceName = node.Interfaces[i];
            hashCode.Add(GetHashCode(interfaceName));
        }

        for (var i = 0; i < node.Fields.Count; i++)
        {
            var field = node.Fields[i];
            hashCode.Add(GetHashCode(field));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(InterfaceTypeExtensionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));

        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];
            hashCode.Add(GetHashCode(directive));
        }

        for (var i = 0; i < node.Interfaces.Count; i++)
        {
            var interfaceName = node.Interfaces[i];
            hashCode.Add(GetHashCode(interfaceName));
        }

        for (var i = 0; i < node.Fields.Count; i++)
        {
            var field = node.Fields[i];
            hashCode.Add(GetHashCode(field));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(IntValueNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
#if NETSTANDARD2_0
        HashCodeExtensions.AddBytes(ref hashCode, node.AsSpan());
#else
        hashCode.AddBytes(node.AsSpan());
#endif
        return hashCode.ToHashCode();
    }

    private int GetHashCode(IValueNode? node)
    {
        if (node is null)
        {
            return 0;
        }

        return node.Kind switch
        {
            SyntaxKind.StringValue => GetHashCode((StringValueNode)node),
            SyntaxKind.IntValue => GetHashCode((IntValueNode)node),
            SyntaxKind.FloatValue => GetHashCode((FloatValueNode)node),
            SyntaxKind.BooleanValue => GetHashCode((BooleanValueNode)node),
            SyntaxKind.EnumValue => GetHashCode((EnumValueNode)node),
            SyntaxKind.ObjectValue => GetHashCode((ObjectValueNode)node),
            SyntaxKind.ListValue => GetHashCode((ListValueNode)node),
            SyntaxKind.NullValue => GetHashCode((NullValueNode)node),
            SyntaxKind.Variable => GetHashCode((VariableNode)node),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private int GetHashCode(ListTypeNode node)
        => HashCode.Combine(node.Kind, GetHashCode(node.Type));

    private int GetHashCode(ListValueNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);

        for (var i = 0; i < node.Items.Count; i++)
        {
            var item = node.Items[i];
            hashCode.Add(GetHashCode(item));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(NamedTypeNode? node)
        => node is null ? 0 : HashCode.Combine(node.Kind, GetHashCode(node.Name));

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

        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];
            hashCode.Add(GetHashCode(directive));
        }

        for (var i = 0; i < node.Interfaces.Count; i++)
        {
            var interfaceName = node.Interfaces[i];
            hashCode.Add(GetHashCode(interfaceName));
        }

        for (var i = 0; i < node.Fields.Count; i++)
        {
            var field = node.Fields[i];
            hashCode.Add(GetHashCode(field));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(ObjectTypeExtensionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));

        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];
            hashCode.Add(GetHashCode(directive));
        }

        for (var i = 0; i < node.Interfaces.Count; i++)
        {
            var interfaceName = node.Interfaces[i];
            hashCode.Add(GetHashCode(interfaceName));
        }

        for (var i = 0; i < node.Fields.Count; i++)
        {
            var field = node.Fields[i];
            hashCode.Add(GetHashCode(field));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(ObjectValueNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);

        for (var i = 0; i < node.Fields.Count; i++)
        {
            var field = node.Fields[i];
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

        for (var i = 0; i < node.VariableDefinitions.Count; i++)
        {
            var variableDefinition = node.VariableDefinitions[i];
            hashCode.Add(GetHashCode(variableDefinition));
        }

        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];
            hashCode.Add(GetHashCode(directive));
        }

        hashCode.Add(GetHashCode(node.SelectionSet));

        return hashCode.ToHashCode();
    }

    private int GetHashCode(OperationTypeDefinitionNode node)
        => HashCode.Combine(node.Kind, node.Operation, GetHashCode(node.Type));

    private int GetHashCode(ScalarTypeDefinitionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));
        hashCode.Add(GetHashCode(node.Description));

        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];
            hashCode.Add(GetHashCode(directive));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(ScalarTypeExtensionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));

        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];
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

        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];
            hashCode.Add(GetHashCode(directive));
        }

        for (var i = 0; i < node.OperationTypes.Count; i++)
        {
            var operationType = node.OperationTypes[i];
            hashCode.Add(GetHashCode(operationType));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(SchemaExtensionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);

        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];
            hashCode.Add(GetHashCode(directive));
        }

        for (var i = 0; i < node.OperationTypes.Count; i++)
        {
            var operationType = node.OperationTypes[i];
            hashCode.Add(GetHashCode(operationType));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(SelectionSetNode? node)
    {
        if (node is null)
        {
            return 0;
        }

        var hashCode = new HashCode();
        hashCode.Add(node.Kind);

        for (var i = 0; i < node.Selections.Count; i++)
        {
            hashCode.Add(GetHashCode(node.Selections[i]));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(StringValueNode? node)
    {
        if (node is null)
        {
            return 0;
        }

        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
#if NETSTANDARD2_0
        HashCodeExtensions.AddBytes(ref hashCode, node.AsSpan());
#else
        hashCode.AddBytes(node.AsSpan());
#endif
        return hashCode.ToHashCode();
    }

    private int GetHashCode(UnionTypeDefinitionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));
        hashCode.Add(GetHashCode(node.Description));

        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];
            hashCode.Add(GetHashCode(directive));
        }

        for (var i = 0; i < node.Types.Count; i++)
        {
            var type = node.Types[i];
            hashCode.Add(GetHashCode(type));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(UnionTypeExtensionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Name));

        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];
            hashCode.Add(GetHashCode(directive));
        }

        for (var i = 0; i < node.Types.Count; i++)
        {
            var type = node.Types[i];
            hashCode.Add(GetHashCode(type));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(VariableDefinitionNode node)
    {
        var hashCode = new HashCode();
        hashCode.Add(node.Kind);
        hashCode.Add(GetHashCode(node.Variable));
        hashCode.Add(GetHashCode(node.Type));
        hashCode.Add(GetHashCode(node.DefaultValue));

        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];
            hashCode.Add(GetHashCode(directive));
        }

        return hashCode.ToHashCode();
    }

    private int GetHashCode(VariableNode node)
        => HashCode.Combine(node.Kind, GetHashCode(node.Name));
}
