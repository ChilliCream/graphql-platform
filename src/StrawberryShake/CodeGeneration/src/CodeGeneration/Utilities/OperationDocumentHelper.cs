using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.Utilities;

/// <summary>
/// Merges all documents and create one query document per operation.
/// </summary>
internal static class OperationDocumentHelper
{
    /// <summary>
    /// Merges the documents and creates operation documents that
    /// can be used for the actual requests.
    /// </summary>
    /// <param name="documents">
    /// The GraphQL documents.
    /// </param>
    /// <param name="schema">
    /// The schema to validate queries against.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static async ValueTask<OperationDocuments> CreateOperationDocumentsAsync(
        IEnumerable<DocumentNode> documents,
        ISchema? schema = null)
    {
        if (documents is null)
        {
            throw new ArgumentNullException(nameof(documents));
        }

        var mergedDocument = MergeDocuments(documents);
        mergedDocument = RemovedUnusedFragmentRewriter.Rewrite(mergedDocument);

        if (schema is not null)
        {
            var validator =
                new ServiceCollection()
                    .AddValidation()
                    .Services
                    .BuildServiceProvider()
                    .GetRequiredService<IDocumentValidatorFactory>()
                    .CreateValidator();

            var result = await validator.ValidateAsync(
                schema,
                mergedDocument,
                new OperationDocumentId("dummy"),
                new Dictionary<string, object?>(),
                false);

            if (result.HasErrors)
            {
                throw new GraphQLException(result.Errors);
            }
        }

        var operationDocs = ExportOperations(mergedDocument);
        return new OperationDocuments(mergedDocument, operationDocs);
    }

    private static DocumentNode MergeDocuments(IEnumerable<DocumentNode> documents)
    {
        var definitions = new List<IDefinitionNode>();

        foreach (var document in documents)
        {
            foreach (var definition in document.Definitions)
            {
                if (definition is OperationDefinitionNode { Name: { } name, } op)
                {
                    name = name.WithValue(GetClassName(name.Value));
                    op = op.WithName(name);
                    definitions.Add(op);
                }
                else
                {
                    definitions.Add(definition);
                }
            }
        }

        ValidateDocument(definitions);

        return new DocumentNode(definitions);
    }

    private static void ValidateDocument(IEnumerable<IDefinitionNode> definitions)
    {
        var operationNames = new HashSet<string>();
        var fragmentNames = new HashSet<string>();

        foreach (var definition in definitions)
        {
            if (definition is OperationDefinitionNode op)
            {
                if (op.Name is null)
                {
                    throw new CodeGeneratorException(
                        ErrorBuilder.New()
                            .SetMessage("All operations must be named.")
                            .AddLocation(op)
                            .Build());
                }

                if (!operationNames.Add(op.Name.Value))
                {
                    throw new CodeGeneratorException(
                        ErrorBuilder.New()
                            .SetMessage(
                                "The operation name `{0}` is not unique.",
                                op.Name.Value)
                            .AddLocation(op)
                            .Build());
                }
            }

            if (definition is FragmentDefinitionNode fd)
            {
                if (!fragmentNames.Add(fd.Name.Value))
                {
                    throw new CodeGeneratorException(
                        ErrorBuilder.New()
                            .SetMessage(
                                "The fragment name `{0}` is not unique.",
                                fd.Name.Value)
                            .AddLocation(fd)
                            .Build());
                }
            }
        }
    }

    private static Dictionary<string, DocumentNode> ExportOperations(DocumentNode document)
    {
        var visitor = new ExtractOperationVisitor();
        var context = new ExtractOperationContext(document);
        var operationDocs = new Dictionary<string, DocumentNode>();

        do
        {
            visitor.Visit(context.Operation, context);

            var definitions = new List<IDefinitionNode> { context.Operation, };
            definitions.AddRange(context.ExportedFragments);
            var operationDoc = new DocumentNode(definitions);
            operationDocs.Add(context.Operation.Name!.Value, operationDoc);
        } while (context.Next());

        return operationDocs;
    }
}
