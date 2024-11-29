#nullable enable

using System.Text;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Attributes;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration.Validation;

internal sealed class IsSelectedPatternValidation : ISchemaValidationRule
{
    public void Validate(
        IDescriptorContext context,
        ISchema schema,
        ICollection<ISchemaError> errors)
    {
        if (!context.ContextData.TryGetValue(WellKnownContextData.PatternValidationTasks, out var value))
        {
            return;
        }

        foreach (var pattern in (List<IsSelectedPattern>)value!)
        {
            var objectField = schema.QueryType.Fields[pattern.FieldName];
            var validationContext = new ValidateIsSelectedPatternContext(schema, objectField, pattern.Pattern);
            ValidateIsSelectedPatternVisitor.Instance.Visit(pattern.Pattern, validationContext);

            if (validationContext.Error is not null)
            {
                errors.Add(validationContext.Error);
            }
        }
    }

    private sealed class ValidateIsSelectedPatternVisitor : SyntaxWalker<ValidateIsSelectedPatternContext>
    {
        protected override ISyntaxVisitorAction Enter(FieldNode node, ValidateIsSelectedPatternContext context)
        {
            var field = context.Field.Peek();
            var typeContext = context.TypeContext.Peek() ?? field.Type.NamedType();

            if (typeContext is IComplexOutputType complexOutputType)
            {
                if (complexOutputType.Fields.TryGetField(node.Name.Value, out var objectField))
                {
                    if (node.SelectionSet is not null)
                    {
                        context.TypeContext.Push(null);
                        context.Field.Push(objectField);
                    }

                    return base.Enter(node, context);
                }

                var message = new StringBuilder();
                message.Append("The specified pattern on field `");
                message.Append(context.Root.Coordinate.ToString());
                message.AppendLine("` is invalid:");
                message.AppendLine($"`{context.Pattern}`");
                message.AppendLine();
                message.Append("The field ");
                message.Append($"`{node.Name.Value}`");
                message.Append(" does not exist on type ");
                message.Append($"`{typeContext.Name}`.");

                context.Error =
                    SchemaErrorBuilder.New()
                        .SetMessage(message.ToString())
                        .AddSyntaxNode(node)
                        .Build();
                return Break;
            }
            else
            {
                var message = new StringBuilder();
                message.Append("The specified pattern on field `");
                message.Append(context.Root.Coordinate.ToString());
                message.AppendLine("` is invalid:");
                message.AppendLine($"`{context.Pattern}`");
                message.AppendLine();
                message.Append("The field declaring type ");
                message.Append($"`{typeContext.Name}`");
                message.Append(" must be a object type or interface type.");

                context.Error =
                    SchemaErrorBuilder.New()
                        .SetMessage(message.ToString())
                        .AddSyntaxNode(node)
                        .Build();

                return Break;
            }
        }

        protected override ISyntaxVisitorAction Leave(FieldNode node, ValidateIsSelectedPatternContext context)
        {
            if (node.SelectionSet is not null)
            {
                context.TypeContext.Pop();
                context.Field.Pop();
            }

            return base.Leave(node, context);
        }

        protected override ISyntaxVisitorAction Enter(InlineFragmentNode node, ValidateIsSelectedPatternContext context)
        {
            if (node.TypeCondition is not null)
            {
                var type = context.Schema.GetType<INamedType>(node.TypeCondition.Name.Value);
                var field = context.Field.Peek();

                if (!type.IsAssignableFrom(field.Type.NamedType()))
                {
                    var message = new StringBuilder();
                    message.Append("The specified pattern on field `");
                    message.Append(context.Root.Coordinate.ToString());
                    message.AppendLine("` is invalid:");
                    message.AppendLine($"`{context.Pattern}`");
                    message.AppendLine();
                    message.Append("The type condition ");
                    message.Append($"`{type.Name}`");
                    message.Append(" of the inline fragment is not assignable from the parent field type ");
                    message.Append($"`{field.Type.NamedType().Name}`.");

                    context.Error =
                        SchemaErrorBuilder.New()
                            .SetMessage(message.ToString())
                            .AddSyntaxNode(node)
                            .Build();
                    return Break;
                }

                context.TypeContext.Push(type);
            }

            return base.Enter(node, context);
        }

        protected override ISyntaxVisitorAction Leave(InlineFragmentNode node, ValidateIsSelectedPatternContext context)
        {
            if (node.TypeCondition is not null)
            {
                context.TypeContext.Pop();
            }

            return base.Leave(node, context);
        }

        public static ValidateIsSelectedPatternVisitor Instance { get; } = new();
    }

    private sealed class ValidateIsSelectedPatternContext
    {
        public ValidateIsSelectedPatternContext(ISchema schema, IObjectField field, SelectionSetNode pattern)
        {
            Schema = schema;
            Root = field;
            Pattern = pattern;

            Field.Push(field);
            TypeContext.Push(null);
        }

        public ISchema Schema { get; }

        public IObjectField Root { get; }

        public SelectionSetNode Pattern { get; }

        public Stack<IOutputField> Field { get; } = new();

        public Stack<INamedType?> TypeContext { get; } = new();

        public ISchemaError? Error { get; set; }
    }
}
