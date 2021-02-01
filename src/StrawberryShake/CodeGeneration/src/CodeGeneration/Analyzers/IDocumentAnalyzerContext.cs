using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;

#nullable enable

namespace StrawberryShake.CodeGeneration.Analyzers
{
    internal interface IDocumentAnalyzerContext
    {
        ISchema Schema { get; }

        DocumentNode Document { get; }

        ObjectType OperationType { get; }

        OperationDefinitionNode OperationDefinition { get; }

        NameString OperationName { get; }

        Path RootPath { get; }

        Queue<FieldSelection> Fields { get; }

        IReadOnlyCollection<ITypeModel> TypeModels { get; }

        IReadOnlyDictionary<SelectionSetInfo, SelectionSetNode> SelectionSets { get; }

        NameString ResolveTypeName(
            NameString proposedName);

        NameString ResolveTypeName(
            NameString proposedName,
            ISyntaxNode syntaxNode,
            IReadOnlyList<string>? additionalNamePatterns = null);

        SelectionSetVariants CollectFields(
            SelectionSetNode selectionSet,
            INamedOutputType type,
            Path path);

        bool TryGetModel<T>(
            NameString name,
            [NotNullWhen(true)] out T? typeModel)
            where T : ITypeModel;

        void RegisterModel(NameString name, ITypeModel typeModel);

        void RegisterType(INamedType type);

        void RegisterSelectionSet(
            INamedType namedType, 
            SelectionSetNode from, 
            SelectionSetNode to);

        IEnumerable<OutputTypeModel> GetImplementations(
            OutputTypeModel outputTypeModel);
    }
}
