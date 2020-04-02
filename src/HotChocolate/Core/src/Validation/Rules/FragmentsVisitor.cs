using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    /// <summary>
    /// Fragment definitions are referenced in fragment spreads by name.
    /// To avoid ambiguity, each fragment’s name must be unique within a
    /// document.
    ///
    /// http://spec.graphql.org/June2018/#sec-Fragment-Name-Uniqueness
    ///
    /// AND
    ///
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
    ///
    /// AND
    ///
    /// Named fragment spreads must refer to fragments defined within the
    /// document.
    ///
    /// It is a validation error if the target of a spread is not defined.
    ///
    /// http://spec.graphql.org/June2018/#sec-Fragment-spread-target-defined
    ///
    /// AND
    ///
    /// The graph of fragment spreads must not form any cycles including
    /// spreading itself. Otherwise an operation could infinitely spread or
    /// infinitely execute on cycles in the underlying data.
    ///
    /// http://spec.graphql.org/June2018/#sec-Fragment-spreads-must-not-form-cycles
    ///
    /// AND
    ///
    /// Fragments must be specified on types that exist in the schema.
    /// This applies for both named and inline fragments.
    /// If they are not defined in the schema, the query does not validate.
    ///
    /// http://spec.graphql.org/June2018/#sec-Fragment-Spread-Type-Existence
    /// </summary>
    internal sealed class FragmentsVisitor : TypeDocumentValidatorVisitor
    {
        protected override ISyntaxVisitorAction Enter(
            DocumentNode node,
            IDocumentValidatorContext context)
        {
            context.Names.Clear();

            for (int i = 0; i < node.Definitions.Count; i++)
            {
                IDefinitionNode definition = node.Definitions[i];
                if (definition.Kind == NodeKind.FragmentDefinition)
                {
                    string fragmentName = ((FragmentDefinitionNode)definition).Name.Value;
                    if (!context.Names.Add(fragmentName))
                    {
                        context.Errors.Add(
                            ErrorBuilder.New()
                                .SetMessage(
                                    "There are multiple fragments with the name " +
                                    $"`{fragmentName}`.")
                                .AddLocation(definition)
                                .SetExtension("fragment", fragmentName)
                                .SpecifiedBy("sec-Fragment-Name-Uniqueness")
                                .Build());
                    }
                }
            }

            context.Names.Clear();

            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            DocumentNode node,
            IDocumentValidatorContext context)
        {
            foreach (string fragmentName in context.Fragments.Keys)
            {
                if (context.Names.Add(fragmentName))
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
            }

            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            FragmentDefinitionNode node,
            IDocumentValidatorContext context)
        {
            context.Names.Add(node.Name.Value);
            ValidateTypeCondition(node, node.TypeCondition, context);
            ValidateFragmentSpreadIsPossible(node, context);
            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            InlineFragmentNode node,
            IDocumentValidatorContext context)
        {
            ValidateTypeCondition(node, node.TypeCondition, context);
            ValidateFragmentSpreadIsPossible(node, context);
            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            FragmentSpreadNode node,
            IDocumentValidatorContext context)
        {
            string fragmentName = node.Name.Value;

            if (context.Fragments.TryGetValue(fragmentName, out FragmentDefinitionNode? fragment))
            {
                if (context.Path.Contains(fragment))
                {
                    context.Errors.Add(
                        ErrorBuilder.New()
                            .SetMessage(
                                "The graph of fragment spreads must not form any " +
                                "cycles including spreading itself. Otherwise an " +
                                "operation could infinitely spread or infinitely " +
                                "execute on cycles in the underlying data.")
                            .AddLocation(node)
                            .SetPath(context.CreateErrorPath())
                            .SetExtension("fragment", fragmentName)
                            .SpecifiedBy("sec-Fragment-spreads-must-not-form-cycles")
                            .Build());
                }
            }
            else
            {
                context.Errors.Add(
                    ErrorBuilder.New()
                        .SetMessage("The specified fragment `{0}` does not exist.", fragmentName)
                        .AddLocation(node)
                        .SetPath(context.CreateErrorPath())
                        .SetExtension("fragment", fragmentName)
                        .SpecifiedBy("sec-Fragment-spread-target-defined")
                        .Build());
            }
            return Continue;
        }

        private void ValidateFragmentSpreadIsPossible(
            ISyntaxNode node,
            IDocumentValidatorContext context)
        {
            if (context.Types.Count > 1 &&
                context.Types.TryPeek(out IType type) &&
                type.IsComplexType())
            {
                INamedType typeCondition = type.NamedType();
                INamedType parentType = context.Types[context.Types.Count - 2].NamedType();

                if (!IsCompatibleType(parentType, typeCondition))
                {
                    context.Errors.Add(
                        ErrorBuilder.New()
                            .SetMessage(
                                "The parent type does not match the type condition on the fragment.")
                            .AddLocation(node)
                            .SetPath(context.CreateErrorPath())
                            .SetExtension("typeCondition", typeCondition.Visualize())
                            .SetFragmentName(node)
                            .SpecifiedBy("sec-Fragment-spread-is-possible")
                            .Build());
                }
            }
        }

        private static bool IsCompatibleType(INamedType parentType, INamedType typeCondition)
        {
            if (parentType.IsAssignableFrom(typeCondition))
            {
                return true;
            }
            else if (parentType.Kind == TypeKind.Object &&
                typeCondition.Kind == TypeKind.Interface &&
                parentType is ObjectType o &&
                typeCondition is InterfaceType i &&
                o.IsImplementing(i))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void ValidateTypeCondition(
            ISyntaxNode node,
            NamedTypeNode? typeCondition,
            IDocumentValidatorContext context)
        {
            if (context.IsInError.PeekOrDefault(true))
            {
                if (typeCondition is { } &&
                    !context.Schema.TryGetType<INamedType>(typeCondition.Name.Value, out _))
                {
                    context.Errors.Add(
                        ErrorBuilder.New()
                            .SetMessage("Unknown type `{0}`.", typeCondition.Name.Value)
                            .AddLocation(node)
                            .SetPath(context.CreateErrorPath())
                            .SetExtension("typeCondition", typeCondition.Name.Value)
                            .SetFragmentName(node)
                            .SpecifiedBy("sec-Fragment-Spread-Type-Existence")
                            .Build());
                }
            }
            else if (context.Types.TryPeek(out IType type) && !type.IsCompositeType())
            {
                context.Errors.Add(
                    ErrorBuilder.New()
                        .SetMessage(
                            "Fragments can only be declared on unions, interfaces, " +
                            "and objects.")
                        .AddLocation(node)
                        .SetPath(context.CreateErrorPath())
                        .SetExtension("typeCondition", type.Visualize())
                        .SetFragmentName(node)
                        .SpecifiedBy("sec-Fragments-On-Composite-Types")
                        .Build());
            }
        }
    }
}
