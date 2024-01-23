using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Utilities;

namespace HotChocolate.Skimmed;

public static class Refactor
{
    public static bool RenameMember(this Schema schema, SchemaCoordinate coordinate, string newName)
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

        if (schema.TryGetMember<IHasName>(coordinate, out var member))
        {
            if (member is INamedType nt)
            {
                schema.Types.Remove(nt);
                member.Name = newName;
                schema.Types.Add(nt);
            }
            else if (member is DirectiveType dt)
            {
                schema.DirectiveTypes.Remove(dt);
                member.Name = newName;
                schema.DirectiveTypes.Add(dt);
            }
            else
            {
                member.Name = newName;
            }

            return true;
        }

        return false;
    }

    public static bool RemoveMember(
        this Schema schema,
        SchemaCoordinate coordinate,
        bool onRequiredRemoveParent = false)
    {
        if (schema is null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        if (coordinate.OfDirective)
        {
            if (schema.DirectiveTypes.TryGetDirective(coordinate.Name, out var directive))
            {
                if (coordinate.ArgumentName is null)
                {
                    schema.DirectiveTypes.Remove(directive);

                    var rewriter = new RemoveDirectiveRewriter();
                    rewriter.VisitSchema(schema, directive);
                    return true;
                }

                if (directive.Arguments.TryGetField(coordinate.ArgumentName, out var arg))
                {
                    if (arg.Type.Kind is TypeKind.NonNull && arg.DefaultValue is null && onRequiredRemoveParent)
                    {
                        schema.DirectiveTypes.Remove(directive);

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
                    var enumType = (EnumType) type;

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
                    var inputType = (InputObjectType) type;

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

            var complexType = (ComplexType) type;

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
        this Schema schema,
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

        if (schema.TryGetMember<IHasDirectives>(coordinate, out var member))
        {
            member.Directives.Add(directive);
            return true;
        }

        return false;
    }

    private sealed class RemoveDirectiveRewriter : SchemaVisitor<DirectiveType>
    {
        private readonly List<Directive> _remove = [];

        public override void VisitDirectives(DirectiveCollection directives, DirectiveType directiveType)
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
        : SchemaVisitor<(DirectiveType Type, string Arg)>
    {
        private readonly List<(Directive, Directive)> _replace = [];

        public override void VisitDirectives(
            DirectiveCollection directives,
            (DirectiveType Type, string Arg) context)
        {
            foreach (var directive in directives)
            {
                if (ReferenceEquals(context.Type, directive.Type))
                {
                    var arguments = new List<Argument>();

                    foreach (var argument in directive.Arguments)
                    {
                        if (!argument.Name.EqualsOrdinal(context.Arg))
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

    private sealed class RemoveTypeRewriter : SchemaVisitor<INamedType>
    {
        // note: by removing fields this could clash with directive arguments
        // we should make this more robust and also remove these.
        private readonly List<OutputField> _removeOutputFields = [];
        private readonly List<InputField> _removeInputFields = [];

        public override void VisitOutputFields(FieldCollection<OutputField> fields, INamedType context)
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

        public override void VisitInputFields(FieldCollection<InputField> fields, INamedType context)
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

        public override void VisitObjectType(ObjectType type, INamedType context)
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

        public override void VisitUnionType(UnionType type, INamedType context)
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

    private sealed class RemoveEnumValueRewriter : SchemaVisitor<(EnumType Type, EnumValue Value)>
    {
        public override void VisitInputField(InputField field, (EnumType Type, EnumValue Value) context)
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
                if (node.Value.EqualsOrdinal(context.Value))
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

        private class RewriterContext : ISyntaxVisitorContext
        {
            public RewriterContext(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }
    }

    private sealed class RemoveInputFieldRewriter
        : SchemaVisitor<(InputObjectType Type, InputField Field)>
    {
        public override void VisitInputField(InputField field, (InputObjectType Type, InputField Field) context)
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
                    if (!item.Name.Value.EqualsOrdinal(context.Name))
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

        private class RewriterContext : ISyntaxVisitorContext
        {
            public RewriterContext(string name)
            {
                Name = name;
            }

            public string Name { get; }
        }
    }
}