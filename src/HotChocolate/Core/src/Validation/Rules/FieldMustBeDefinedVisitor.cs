using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;

namespace HotChocolate.Validation.Rules
{
    /// <summary>
    /// The target field of a field selection must be defined on the scoped
    /// type of the selection set. There are no limitations on alias names.
    ///
    /// http://spec.graphql.org/June2018/#sec-Field-Selections-on-Objects-Interfaces-and-Unions-Types
    /// </summary>
    internal sealed class FieldMustBeDefinedVisitor : TypeDocumentValidatorVisitor2
    {
        protected override ISyntaxVisitorAction Enter(
            FieldNode node,
            IDocumentValidatorContext context)
        {
            context.Names.Clear();

            if (IntrospectionFields.TypeName.Equals(node.Name.Value))
            {
                return Skip;
            }
            else if (context.Types.TryPeek(out IType type) &&
                type.NamedType() is IComplexOutputType ct)
            {
                if (ct.Fields.TryGetField(node.Name.Value, out IOutputField of))
                {
                    context.OutputFields.Push(of);
                    context.Types.Push(of.Type);
                    return Continue;
                }
                else
                {
                    context.Errors.Add(context.FieldDoesNotExist(node, ct));
                    return Skip;
                }
            }
            else
            {
                context.UnexpectedErrorsDetected = true;
                return Skip;
            }
        }

        protected override ISyntaxVisitorAction Enter(
            SelectionSetNode node,
            IDocumentValidatorContext context)
        {
            if (context.Types.TryPeek(out IType type) &&
                type.NamedType().Kind == TypeKind.Union &&
                HasFields(node))
            {
                context.Errors.Add(context.UnionFieldError(node, (UnionType)type));
                return Skip;
            }
            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            FieldNode node,
            IDocumentValidatorContext context)
        {
            context.OutputFields.Pop();
            context.Types.Pop();
            return Continue;
        }

        private static bool HasFields(SelectionSetNode selectionSet)
        {
            for (int i = 0; i < selectionSet.Selections.Count; i++)
            {
                ISelectionNode selection = selectionSet.Selections[i];
                if (selection.Kind == NodeKind.Field)
                {
                    if (!IsTypeNameField(((FieldNode)selection).Name.Value))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool IsTypeNameField(NameString fieldName)
        {
            return fieldName.Equals(IntrospectionFields.TypeName);
        }
    }
}
