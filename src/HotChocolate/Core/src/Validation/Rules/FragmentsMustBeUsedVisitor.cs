using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    /// <summary>
    /// Defined fragments must be used within a document.
    ///
    /// http://spec.graphql.org/June2018/#sec-Fragments-Must-Be-Used
    ///
    /// AND
    ///
    /// Fragments can only be declared on unions, interfaces, and objects.
    /// They are invalid on scalars.
    /// They can only be applied on non‐leaf fields.
    /// This rule applies to both inline and named fragments.
    ///
    /// http://spec.graphql.org/June2018/#sec-Fragments-On-Composite-Types
    ///
    /// AND
    ///
    /// Fragments are declared on a type and will only apply when the
    /// runtime object type matches the type condition.
    ///
    /// They also are spread within the context of a parent type.
    ///
    /// A fragment spread is only valid if its type condition could ever
    /// apply within the parent type.
    ///
    /// http://spec.graphql.org/June2018/#sec-Fragment-spread-is-possible
    /// </summary>
    internal sealed class FragmentsMustBeUsedVisitor : TypeDocumentValidatorVisitor
    {
        protected override ISyntaxVisitorAction Enter(
            DocumentNode node,
            IDocumentValidatorContext context)
        {
            context.Names.Clear();
            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            DocumentNode node,
            IDocumentValidatorContext context)
        {
            context.Names.ExceptWith(context.Fragments.Keys);

            foreach (string fragmentName in context.Names)
            {
                context.Errors.Add(
                    ErrorBuilder.New()
                        .SetMessage(
                            $"The specified fragment `{fragmentName}` " +
                            "is not used within the current document.")
                        .AddLocation(context.Fragments[fragmentName])
                        .SetPath(context.CreateErrorPath())
                        .SetExtension("fragment", fragmentName)
                        .SpecifiedBy("sec-Fragments-Must-Be-Used")
                        .Build());
            }

            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            FragmentDefinitionNode node,
            IDocumentValidatorContext context)
        {
            context.Names.Add(node.Name.Value);
            ValidateTypeCondition(node, context);
            ValidateFragmentSpreadIsPossible(node, context);
            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            InlineFragmentNode node,
            IDocumentValidatorContext context)
        {
            ValidateTypeCondition(node, context);
            ValidateFragmentSpreadIsPossible(node, context);
            return Continue;
        }

        private void ValidateFragmentSpreadIsPossible(
            ISyntaxNode node,
            IDocumentValidatorContext context)
        {
            if (context.Types.Count > 1)
            {
                INamedType typeCondition = context.Types[context.Types.Count - 1].NamedType();
                INamedType parentType = context.Types[context.Types.Count - 2].NamedType();

                if (!parentType.IsAssignableFrom(typeCondition))
                {
                    IErrorBuilder builder =
                    ErrorBuilder.New()
                        .SetMessage(
                            "The parent type does not match the type condition on the fragment.")
                        .AddLocation(node)
                        .SetPath(context.CreateErrorPath())
                        .SetExtension("typeCondition", typeCondition.Visualize());

                    if (node.Kind == NodeKind.FragmentDefinition)
                    {
                        builder.SetExtension("fragment", ((FragmentDefinitionNode)node).Name.Value);
                    }

                    context.Errors.Add(
                        builder
                            .SpecifiedBy("sec-Fragment-spread-is-possible")
                            .Build());
                }
            }
        }

        private void ValidateTypeCondition(
            ISyntaxNode node,
            IDocumentValidatorContext context)
        {
            if (!context.IsInError &&
                context.Types.TryPeek(out IType typeCondition) &&
                typeCondition.IsCompositeType())
            {
                IErrorBuilder builder =
                    ErrorBuilder.New()
                        .SetMessage(
                            "Fragments can only be declared on unions, interfaces, " +
                            "and objects.")
                        .AddLocation(node)
                        .SetPath(context.CreateErrorPath())
                        .SetExtension("typeCondition", typeCondition.Visualize());

                if (node.Kind == NodeKind.FragmentDefinition)
                {
                    builder.SetExtension("fragment", ((FragmentDefinitionNode)node).Name.Value);
                }

                context.Errors.Add(
                    builder
                        .SpecifiedBy("sec-Fragments-On-Composite-Types")
                        .Build());
            }
        }
    }
}
