using HotChocolate;
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
    /// <returns>The merged operation documents.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static OperationDocuments CreateOperationDocuments(
        IEnumerable<DocumentNode> documents)
    {
        ArgumentNullException.ThrowIfNull(documents);

        var mergedDocument = RemovedUnusedFragmentRewriter.Rewrite(MergeDocuments(documents));

        return new OperationDocuments(mergedDocument, ExportOperations(mergedDocument));
    }

    /// <summary>
    /// Merges the documents, validates them against the schema and creates
    /// operation documents that can be used for the actual requests.
    /// </summary>
    /// <param name="documents">
    /// The GraphQL documents.
    /// </param>
    /// <param name="schema">
    /// The schema to validate queries against.
    /// </param>
    /// <param name="enableCovariantFieldMerging">
    /// Defines if fields whose return types differ only in nullability can be merged.
    /// </param>
    /// <returns>The merged operation documents.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="GraphQLException">
    /// The merged document is not valid against the schema.
    /// </exception>
    public static OperationDocuments CreateOperationDocuments(
        IEnumerable<DocumentNode> documents,
        Schema schema,
        bool enableCovariantFieldMerging)
    {
        ArgumentNullException.ThrowIfNull(documents);
        ArgumentNullException.ThrowIfNull(schema);

        var mergedDocument = RemovedUnusedFragmentRewriter.Rewrite(MergeDocuments(documents));

        var validator =
            DocumentValidatorBuilder.New()
                .AddDefaultRules()
                .ModifyOptions(o => o.EnableCovariantFieldMerging = enableCovariantFieldMerging)
                .Build();

        var result = validator.Validate(schema, mergedDocument);

        if (result.HasErrors)
        {
            throw new GraphQLException(result.Errors);
        }

        return new OperationDocuments(mergedDocument, ExportOperations(mergedDocument));
    }

    private static DocumentNode MergeDocuments(IEnumerable<DocumentNode> documents)
    {
        var definitions = new List<IDefinitionNode>();

        foreach (var document in documents)
        {
            foreach (var definition in document.Definitions)
            {
                if (definition is OperationDefinitionNode { Name: { } name } op)
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

            var definitions = new List<IDefinitionNode> { context.Operation };
            definitions.AddRange(context.ExportedFragments);
            var operationDoc = new DocumentNode(definitions);
            operationDocs.Add(context.Operation.Name!.Value, operationDoc);
        } while (context.Next());

        return operationDocs;
    }
}
