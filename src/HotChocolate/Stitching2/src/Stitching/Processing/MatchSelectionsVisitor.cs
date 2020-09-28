using System.Diagnostics;
using System.Linq;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Processing
{
    public class MatchSelectionsVisitor : SyntaxWalker<MatchSelectionsContext>
    {
        protected override ISyntaxVisitorAction Enter(FieldNode node, MatchSelectionsContext context)
        {
            IOutputType outputType = context.Types.Peek();
            Field field = GetField(context, node, outputType);

            if (context.Selections.TryGetValue(field.FieldName, out ISelection? selection))
            {
                context.Count++;

                if (selection.Field.Type.IsCompositeType())
                {
                    Debug.Assert(
                        selection.SelectionSet is not null,
                        "A composite type must have a selection set.");

                    foreach (IObjectType possibleType in
                        context.Operation.GetPossibleTypes(selection.SelectionSet!))
                    {
                        ISelectionSet selectionSet =
                            context.Operation.GetSelectionSet(selection.SelectionSet!, possibleType);
                        return base.Enter(node, context.Branch(possibleType, selectionSet));
                    }
                }
            }

            return Skip;
        }

        private IOutputType GetReturnType(MatchSelectionsContext context, FieldNode node, IOutputType type)
        {
            if (GetDirective(node, "_return") is DirectiveNode directive &&
                directive.Arguments.Count == 1 &&
                directive.Arguments[0] is { Name: { Value: "type" } } argument &&
                argument.Value is StringValueNode value)
            {
                ITypeNode typeSyntax = Utf8GraphQLParser.Syntax.ParseTypeReference(value.Value);
                NamedTypeNode namedTypeSyntax = typeSyntax.NamedType();
                var named = context.Schema.GetType<INamedOutputType>(namedTypeSyntax.Name.Value);
                return (IOutputType)typeSyntax.ToType(named);
            }

            return type;
        }

        private Field GetField(MatchSelectionsContext context, FieldNode node, IOutputType type)
        {
            if (GetDirective(node, "_field") is DirectiveNode directive &&
                directive.Arguments.Count == 2 &&
                directive.Arguments.FirstOrDefault(a => a.Name.Value.Equals("type")) is { } t &&
                directive.Arguments.FirstOrDefault(a => a.Name.Value.Equals("name")) is { } n)
            {
                return new Field(t.Name.Value, n.Name.Value);
            }

            return new Field(type.NamedType().Name, node.Name.Value);
        }

        private DirectiveNode? GetDirective(FieldNode field, string directiveName)
        {
            return field.Directives.FirstOrDefault(t => t.Name.Value.EqualsOrdinal(directiveName));
        }
    }




}
