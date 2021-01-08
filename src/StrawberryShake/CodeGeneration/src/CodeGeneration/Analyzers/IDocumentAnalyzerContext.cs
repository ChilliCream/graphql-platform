using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Utilities;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    internal interface IDocumentAnalyzerContext2
    {
        ISchema Schema { get; }

        ObjectType OperationType { get; }

        OperationDefinitionNode OperationDefinition { get; }

        NameString OperationName { get; }

        Queue<FieldSelection> Fields { get; }

        NameString ResolveTypeName(
            NameString proposedName);

        NameString ResolveTypeName(
            NameString proposedName,
            ISyntaxNode node,
            Path path,
            IReadOnlyList<string>? additionalNamePatterns = null);

        SelectionSetVariants CollectFields(
            SelectionSetNode selectionSet,
            INamedOutputType type,
            Path path);
    }
}
