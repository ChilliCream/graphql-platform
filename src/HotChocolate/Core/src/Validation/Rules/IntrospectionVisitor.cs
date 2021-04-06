using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;

namespace HotChocolate.Validation.Rules
{
    /// <summary>
    /// Validates if introspection is allowed.
    /// </summary>
    internal sealed class IntrospectionVisitor : TypeDocumentValidatorVisitor
    {
        protected override ISyntaxVisitorAction Enter(
            OperationDefinitionNode node,
            IDocumentValidatorContext context)
        {
            if (node.Operation == OperationType.Query &&
                !context.ContextData.ContainsKey(WellKnownContextData.IntrospectionAllowed))
            {
                return base.Enter(node, context);
            }

            return Skip;
        }

        protected override ISyntaxVisitorAction Enter(
            FieldNode node,
            IDocumentValidatorContext context)
        {
            if (IntrospectionFields.TypeName.Equals(node.Name.Value))
            {
                return Skip;
            }

            if (context.Types.TryPeek(out IType type))
            {
                INamedType namedType = type.NamedType();

                if(context.Schema.QueryType == namedType  &&
                    (IntrospectionFields.Schema.Equals(node.Name.Value) ||
                     IntrospectionFields.Type.Equals(node.Name.Value)))
                {
                    context.Errors.Add(context.IntrospectionNotAllowed(node));
                    return Break;
                }

                if (namedType is IComplexOutputType ct)
                {
                    if (ct.Fields.TryGetField(node.Name.Value, out IOutputField? of))
                    {
                        if (node.SelectionSet is null ||
                            node.SelectionSet.Selections.Count == 0 ||
                            of.Type.NamedType().IsLeafType())
                        {
                            return Skip;
                        }

                        context.OutputFields.Push(of);
                        context.Types.Push(of.Type);
                        return Continue;
                    }

                    return Skip;
                }
            }

            context.UnexpectedErrorsDetected = true;
            return Skip;
        }

        protected override ISyntaxVisitorAction Leave(
            FieldNode node,
            IDocumentValidatorContext context)
        {
            context.OutputFields.Pop();
            context.Types.Pop();
            return Continue;
        }
    }
}
