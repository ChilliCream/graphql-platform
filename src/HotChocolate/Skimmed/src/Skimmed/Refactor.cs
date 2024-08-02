using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Skimmed;

public static class Refactor
{
    public static bool RenameMember(this SchemaDefinition schema, SchemaCoordinate coordinate, string newName)
    {
        if (schema is null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        if (string.IsNullOrEmpty(newName))
        {
            throw new ArgumentException(
                "Value cannot be null or empty.",
                nameof(newName));
        }

        if (schema.TryGetMember<INameProvider>(coordinate, out var member))
        {
            if (member is INamedTypeDefinition nt)
            {
                schema.Types.Remove(nt);
                nt.Name = newName;
                schema.Types.Add(nt);
                return true;
            }

            if (member is DirectiveDefinition dt)
            {
                schema.DirectiveDefinitions.Remove(dt);
                dt.Name = newName;
                schema.DirectiveDefinitions.Add(dt);
                return true;
            }

            if (member is IFieldDefinition field)
            {
                // TODO: we need to update the field collection
                field.Name = newName;
                return true;
            }
        }

        return false;
    }

    public static bool RemoveMember(
        this SchemaDefinition schema,
        SchemaCoordinate coordinate,
        bool onRequiredRemoveParent = false)
    {
        if (schema is null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        if (coordinate.OfDirective)
        {
            if (schema.DirectiveDefinitions.TryGetDirective(coordinate.Name, out var directive))
            {
                if (coordinate.ArgumentName is null)
                {
                    schema.DirectiveDefinitions.Remove(directive);

                    var rewriter = new RemoveDirectiveRewriter();
                    rewriter.VisitSchema(schema, directive);
                    return true;
                }

                if (directive.Arguments.TryGetField(coordinate.ArgumentName, out var arg))
                {
                    if (arg.Type.Kind is TypeKind.NonNull && arg.DefaultValue is null && onRequiredRemoveParent)
                    {
                        schema.DirectiveDefinitions.Remove(directive);

                        var rewriter = new RemoveDirectiveRewriter();
                        rewriter.VisitSchema(schema, directive);
                        return true;
                    }
                    else
                    {
                        var rewriter = new RemoveDirectiveArgRewriter();
                        rewriter.VisitSchema(schema, (directive, arg.Name));
                        return true;
                    }
                }
            }

            return false;
        }

        if (schema.Types.TryGetType(coordinate.Name, out var type))
        {
            if (coordinate.MemberName is null)
            {
                schema.Types.Remove(type);
                var rewriter = new RemoveTypeRewriter();
                rewriter.VisitSchema(schema, type);
                return true;
            }

            if (coordinate.ArgumentName is null)
            {
                if (type.Kind is TypeKind.Enum)
                {
                    var enumType = (EnumTypeDefinition) type;

                    if (enumType.Values.TryGetValue(coordinate.MemberName, out var enumValue))
                    {
                        enumType.Values.Remove(enumValue);
                        var rewriter = new RemoveEnumValueRewriter();
                        rewriter.VisitSchema(schema, (enumType, enumValue));
                        return true;
                    }
                }

                if (type.Kind is TypeKind.InputObject)
                {
                    var inputType = (InputObjectTypeDefinition) type;

                    if (inputType.Fields.TryGetField(coordinate.MemberName, out var input))
                    {
                        if (input.Type.Kind is TypeKind.NonNull &&
                            input.DefaultValue is null &&
                            onRequiredRemoveParent)
                        {
                            schema.Types.Remove(type);
                            var rewriter = new RemoveTypeRewriter();
                            rewriter.VisitSchema(schema, type);
                            return true;
                        }
                        else
                        {
                            inputType.Fields.Remove(input);
                            var rewriter = new RemoveInputFieldRewriter();
                            rewriter.VisitSchema(schema, (inputType, input));
                            return true;
                        }
                    }
                }
            }

            if (type.Kind is not TypeKind.Object and not TypeKind.Interface)
            {
                return false;
            }

            var complexType = (ComplexTypeDefinition) type;

            if (complexType.Fields.TryGetField(coordinate.MemberName, out var field))
            {
                if (coordinate.ArgumentName is null)
                {
                    complexType.Fields.Remove(field);
                    return true;
                }

                if (field.Arguments.TryGetField(coordinate.ArgumentName, out var fieldArg))
                {
                    if (fieldArg.Type.Kind is TypeKind.NonNull &&
                        fieldArg.DefaultValue is null &&
                        onRequiredRemoveParent)
                    {
                        complexType.Fields.Remove(field);
                        return true;
                    }
                    else
                    {
                        field.Arguments.Remove(fieldArg);
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public static bool AddDirective(
        this SchemaDefinition schema,
        SchemaCoordinate coordinate,
        Directive directive)
    {
        if (schema is null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        if (directive is null)
        {
            throw new ArgumentNullException(nameof(directive));
        }

        if (schema.TryGetMember<IDirectivesProvider>(coordinate, out var member))
        {
            member.Directives.Add(directive);
            return true;
        }

        return false;
    }

    private sealed class RemoveDirectiveRewriter : SchemaVisitor<DirectiveDefinition>
    {
        private readonly List<Directive> _remove = [];

        public override void VisitDirectives(IDirectiveCollection directives, DirectiveDefinition directiveType)
        {
            foreach (var directive in directives)
            {
                if (ReferenceEquals(directiveType, directive.Type))
                {
                    _remove.Add(directive);
                }
            }

            foreach (var directive in _remove)
            {
                directives.Remove(directive);
            }

            _remove.Clear();
        }
    }

    private sealed class RemoveDirectiveArgRewriter
        : SchemaVisitor<(DirectiveDefinition Type, string Arg)>
    {
        private readonly List<(Directive, Directive)> _replace = [];

        public override void VisitDirectives(
            IDirectiveCollection directives,
            (DirectiveDefinition Type, string Arg) context)
        {
            foreach (var directive in directives)
            {
                if (ReferenceEquals(context.Type, directive.Type))
                {
                    var arguments = new List<ArgumentAssignment>();

                    foreach (var argument in directive.Arguments)
                    {
                        if (!argument.Name.Equals(context.Arg, StringComparison.Ordinal))
                        {
                            arguments.Add(argument);
                        }
                    }

                    _replace.Add((directive, new Directive(context.Type, arguments)));
                }
            }

            foreach (var (current, updated) in _replace)
            {
                directives.Replace(current, updated);
            }

            _replace.Clear();
        }
    }

    private sealed class RemoveTypeRewriter : SchemaVisitor<INamedTypeDefinition>
    {
        // note: by removing fields this could clash with directive arguments
        // we should make this more robust and also remove these.
        private readonly List<OutputFieldDefinition> _removeOutputFields = [];
        private readonly List<InputFieldDefinition> _removeInputFields = [];

        public override void VisitOutputFields(
            IFieldDefinitionCollection<OutputFieldDefinition> fields,
            INamedTypeDefinition context)
        {
            foreach (var field in fields)
            {
                if (ReferenceEquals(field.Type.NamedType(), context))
                {
                    _removeOutputFields.Add(field);
                }
            }

            foreach (var field in _removeOutputFields)
            {
                fields.Remove(field);
            }

            foreach (var field in fields)
            {
                VisitOutputField(field, context);
            }
        }

        public override void VisitInputFields(
            IFieldDefinitionCollection<InputFieldDefinition> fields,
            INamedTypeDefinition context)
        {
            foreach (var field in fields)
            {
                if (ReferenceEquals(field.Type.NamedType(), context))
                {
                    _removeInputFields.Add(field);
                }
            }

            foreach (var field in _removeInputFields)
            {
                fields.Remove(field);
            }

            foreach (var field in fields)
            {
                VisitInputField(field, context);
            }
        }

        public override void VisitObjectType(ObjectTypeDefinition type, INamedTypeDefinition context)
        {
            var current = type.Implements.Count - 1;

            while (current >= 0)
            {
                var interfaceType = type.Implements[current];

                if (ReferenceEquals(interfaceType, context))
                {
                    type.Implements.RemoveAt(current);
                }

                current--;
            }

            base.VisitObjectType(type, context);
        }

        public override void VisitUnionType(UnionTypeDefinition type, INamedTypeDefinition context)
        {
            var current = type.Types.Count - 1;

            while (current >= 0)
            {
                var interfaceType = type.Types[current];

                if (ReferenceEquals(interfaceType, context))
                {
                    type.Types.RemoveAt(current);
                }

                current--;
            }

            base.VisitUnionType(type, context);
        }
    }

    private sealed class RemoveEnumValueRewriter : SchemaVisitor<(EnumTypeDefinition Type, EnumValue Value)>
    {
        public override void VisitInputField(InputFieldDefinition field, (EnumTypeDefinition Type, EnumValue Value) context)
        {
            if (field.DefaultValue is not null &&
                ReferenceEquals(field.Type.NamedType(), context.Type))
            {
                var rewriter = new ValueRewriter();
                var rewritten = rewriter.Rewrite(
                    field.DefaultValue,
                    new RewriterContext(context.Value.Name));
                field.DefaultValue = (IValueNode?) rewritten;
            }

            base.VisitInputField(field, context);
        }

        private sealed class ValueRewriter : SyntaxRewriter<RewriterContext>
        {
            protected override EnumValueNode? RewriteEnumValue(
                EnumValueNode node,
                RewriterContext context)
            {
                if (node.Value.Equals(context.Value, StringComparison.Ordinal))
                {
                    return null;
                }

                return base.RewriteEnumValue(node, context);
            }

            protected override ListValueNode? RewriteListValue(
                ListValueNode node,
                RewriterContext context)
            {
                var items = new List<IValueNode>();

                foreach (var item in node.Items)
                {
                    var rewritten = RewriteNodeOrDefault(item, context);

                    if (rewritten is not null)
                    {
                        items.Add(item);
                    }
                }

                return items.Count == node.Items.Count
                    ? node
                    : node.WithItems(items);
            }
        }

        private class RewriterContext(string value)
        {
            public string Value { get; } = value;
        }
    }

    private sealed class RemoveInputFieldRewriter : SchemaVisitor<(InputObjectTypeDefinition Type, InputFieldDefinition Field)>
    {
        public override void VisitInputField(InputFieldDefinition field, (InputObjectTypeDefinition Type, InputFieldDefinition Field) context)
        {
            if (field.DefaultValue is not null &&
                ReferenceEquals(field.Type.NamedType(), context.Type))
            {
                var rewriter = new ValueRewriter();
                var rewritten = rewriter.Rewrite(
                    field.DefaultValue,
                    new RewriterContext(field.Name));
                field.DefaultValue = (IValueNode?) rewritten;
            }

            base.VisitInputField(field, context);
        }

        private sealed class ValueRewriter : SyntaxRewriter<RewriterContext>
        {
            protected override ObjectValueNode? RewriteObjectValue(ObjectValueNode node, RewriterContext context)
            {
                var fields = new List<ObjectFieldNode>();

                foreach (var item in node.Fields)
                {
                    if (!item.Name.Value.Equals(context.Name, StringComparison.Ordinal))
                    {
                        var rewritten = RewriteNodeOrDefault(item, context);

                        if (rewritten is not null)
                        {
                            fields.Add(item);
                        }
                    }
                }

                return fields.Count == node.Fields.Count
                    ? node
                    : node.WithFields(fields);
            }
        }

        private class RewriterContext(string name)
        {
            public string Name { get; } = name;
        }
    }
}
