using System;
using HotChocolate.Language;

namespace HotChocolate.Validation.Rules
{
    /// <summary>
    /// GraphQL execution will only consider the executable definitions
    /// Operation and Fragment.
    ///
    /// Type system definitions and extensions are not executable,
    /// and are not considered during execution.
    ///
    /// To avoid ambiguity, a document containing TypeSystemDefinition
    /// is invalid for execution.
    ///
    /// GraphQL documents not intended to be directly executed may
    /// include TypeSystemDefinition.
    ///
    /// http://spec.graphql.org/June2018/#sec-Executable-Definitions
    /// </summary>
    internal sealed class DocumentRule : IDocumentValidatorRule
    {
        public void Validate(IDocumentValidatorContext context, DocumentNode document)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            IDefinitionNode? typeSystemNode = null;

            for (int i = 0; i < document.Definitions.Count; i++)
            {
                IDefinitionNode node = document.Definitions[i];
                if (node.Kind != SyntaxKind.OperationDefinition &&
                    node.Kind != SyntaxKind.FragmentDefinition)
                {
                    typeSystemNode = node;
                    break;
                }
            }

            if (typeSystemNode is { })
            {
                context.Errors.Add(
                    ErrorHelper.TypeSystemDefinitionNotAllowed(
                        context, typeSystemNode));
            }
        }
    }
}
