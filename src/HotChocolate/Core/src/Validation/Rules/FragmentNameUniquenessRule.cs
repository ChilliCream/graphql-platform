using System;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    /// <summary>
    /// Fragment definitions are referenced in fragment spreads by name.
    /// To avoid ambiguity, each fragment’s name must be unique within a
    /// document.
    ///
    /// http://spec.graphql.org/June2018/#sec-Fragment-Name-Uniqueness
    /// </summary>
    internal sealed class FragmentNameUniquenessRule : IDocumentValidatorRule
    {
        public void Validate(IDocumentValidatorContext context, DocumentNode document)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            context.Names.Clear();

            for (int i = 0; i < document.Definitions.Count; i++)
            {
                IDefinitionNode node = document.Definitions[i];
                if (node.Kind == NodeKind.FragmentDefinition)
                {
                    string fragmentName = ((FragmentDefinitionNode)node).Name.Value;
                    if (!context.Names.Add(fragmentName))
                    {
                        context.Errors.Add(
                            ErrorBuilder.New()
                                .SetMessage(
                                    "There are multiple fragments with the name " +
                                    $"`{fragmentName}`.")
                                .AddLocation(node)
                                .SetExtension("fragment", fragmentName)
                                .SpecifiedBy("sec-Fragment-Name-Uniqueness")
                                .Build());
                    }
                }
            }
        }
    }
}
