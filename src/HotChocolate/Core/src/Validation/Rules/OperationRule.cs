using System;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    /// <summary>
    /// GraphQL allows a short‐hand form for defining query operations
    /// when only that one operation exists in the document.
    ///
    /// http://spec.graphql.org/June2018/#sec-Lone-Anonymous-Operation
    ///
    /// AND
    ///
    /// Each named operation definition must be unique within a document
    /// when referred to by its name.
    ///
    /// http://spec.graphql.org/June2018/#sec-Operation-Name-Uniqueness
    /// </summary>
    internal class OperationRule : IDocumentValidatorRule
    {
        public void Validate(
            IDocumentValidatorContext context,
            DocumentNode document)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            bool hasAnonymousOp = false;
            int opCount = 0;
            OperationDefinitionNode? anonymousOp = null;

            context.Names.Clear();

            for (int i = 0; i < document.Definitions.Count; i++)
            {
                IDefinitionNode definition = document.Definitions[i];
                if (definition.Kind == NodeKind.OperationDefinition)
                {
                    opCount++;

                    var operation = (OperationDefinitionNode)definition;

                    if (operation.Name is null)
                    {
                        hasAnonymousOp = true;
                        anonymousOp = operation;
                    }
                    else if (!context.Names.Add(operation.Name.Value))
                    {
                        context.Errors.Add(context.OperationNameNotUnique(
                            operation, operation.Name.Value));
                    }
                }
            }

            if (hasAnonymousOp && opCount > 1)
            {
                context.Errors.Add(context.OperationAnonymousMoreThanOne(anonymousOp!, opCount));
            }
        }
    }
}
