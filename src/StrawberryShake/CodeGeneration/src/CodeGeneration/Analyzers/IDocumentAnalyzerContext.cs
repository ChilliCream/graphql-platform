using System.Diagnostics.CodeAnalysis;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using Path = HotChocolate.Path;

namespace StrawberryShake.CodeGeneration.Analyzers;

public interface IDocumentAnalyzerContext
{
    ISchema Schema { get; }

    DocumentNode Document { get; }

    ObjectType OperationType { get; }

    OperationDefinitionNode OperationDefinition { get; }

    string OperationName { get; }

    Path RootPath { get; }

    Queue<FieldSelection> Fields { get; }

    IReadOnlyCollection<ITypeModel> TypeModels { get; }

    IReadOnlyDictionary<SelectionSetInfo, SelectionSetNode> SelectionSets { get; }

    string ResolveTypeName(string proposedName);

    string ResolveTypeName(
        string proposedName,
        ISyntaxNode syntaxNode,
        IReadOnlyList<string>? additionalNamePatterns = null);

    SelectionSetVariants CollectFields(
        SelectionSetNode selectionSet,
        INamedOutputType type,
        Path path);

    bool TryGetModel<T>(
        string name,
        [NotNullWhen(true)] out T? typeModel)
        where T : ITypeModel;

    void RegisterModel(string name, ITypeModel typeModel);

    void RegisterType(INamedType type);

    void RegisterSelectionSet(
        INamedType namedType,
        SelectionSetNode from,
        SelectionSetNode to);

    IEnumerable<OutputTypeModel> GetImplementations(
        OutputTypeModel outputTypeModel);
}
