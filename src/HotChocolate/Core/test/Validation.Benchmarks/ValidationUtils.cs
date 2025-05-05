using HotChocolate.Language;
using HotChocolate.Types;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Validation;

public static class ValidationUtils
{
    public static DocumentValidatorContext CreateContext(ISchema schema)
        => new()
        {
            Schema = schema,
            ContextData = new Dictionary<string, object?>(),
        };

    public static void Prepare(this IDocumentValidatorContext context, DocumentNode document)
    {
        context.Fragments.Clear();

        for (var i = 0; i < document.Definitions.Count; i++)
        {
            var definitionNode = document.Definitions[i];
            if (definitionNode.Kind == SyntaxKind.FragmentDefinition)
            {
                var fragmentDefinition = (FragmentDefinitionNode)definitionNode;
                context.Fragments[fragmentDefinition.Name.Value] = fragmentDefinition;
            }
        }
    }
}
