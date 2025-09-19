using HotChocolate.Language;

namespace HotChocolate.Validation.Rules;

/// <summary>
/// <para>
/// GraphQL execution will only consider the executable definitions
/// Operation and Fragment.
/// </para>
/// <para>
/// Type system definitions and extensions are not executable,
/// and are not considered during execution.
/// </para>
/// <para>
/// To avoid ambiguity, a document containing TypeSystemDefinition
/// is invalid for execution.
/// </para>
/// <para>
/// GraphQL documents not intended to be directly executed may
/// include TypeSystemDefinition.
/// </para>
/// <para>https://spec.graphql.org/June2018/#sec-Executable-Definitions</para>
/// </summary>
internal sealed class DocumentRule : IDocumentValidatorRule
{
    public ushort Priority => ushort.MaxValue;

    public bool IsCacheable => true;

    public void Validate(DocumentValidatorContext context, DocumentNode document)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(document);

        IDefinitionNode? typeSystemNode = null;

        for (var i = 0; i < document.Definitions.Count; i++)
        {
            var node = document.Definitions[i];
            if (node.Kind is not SyntaxKind.OperationDefinition
                and not SyntaxKind.FragmentDefinition)
            {
                typeSystemNode = node;
                break;
            }
        }

        if (typeSystemNode is not null)
        {
            context.ReportError(context.TypeSystemDefinitionNotAllowed(typeSystemNode));
        }
    }
}
