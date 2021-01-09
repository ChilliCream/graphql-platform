using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    internal interface IDocumentAnalyzerContext 
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
            ISyntaxNode syntaxNode,
            IReadOnlyList<string>? additionalNamePatterns = null);

        SelectionSetVariants CollectFields(
            SelectionSetNode selectionSet,
            INamedOutputType type,
            Path path);
    }

    public class DocumentAnalyzerContext : IDocumentAnalyzerContext
    {
        private HashSet<NameString> _takenNames = new();
        private Dictionary<ISyntaxNode, NameString> _syntaxNodeNames = new();
        private readonly FieldCollector _fieldCollector;

        public DocumentAnalyzerContext(
            ISchema schema, 
            DocumentNode document)
        {
            Schema = schema;
            OperationDefinition = document.Definitions.OfType<OperationDefinitionNode>().First();
            OperationType = schema.GetOperationType(OperationDefinition.Operation);
            OperationName = OperationDefinition.Name!.Value;

            _fieldCollector = new FieldCollector(schema, document);
        }

        public ISchema Schema { get; }

        public ObjectType OperationType { get; }

        public OperationDefinitionNode OperationDefinition { get; }

        public NameString OperationName { get; }

        public Queue<FieldSelection> Fields { get; } = new Queue<FieldSelection>();

        public SelectionSetVariants CollectFields() =>
            _fieldCollector.CollectFields(
                OperationDefinition.SelectionSet, 
                OperationType, 
                Path.New(OperationName));

        public SelectionSetVariants CollectFields(
            FieldSelection fieldSelection) =>
            CollectFields(
                fieldSelection.SyntaxNode.SelectionSet!,
                (INamedOutputType)fieldSelection.Field.Type.NamedType(),
                fieldSelection.Path);

        public SelectionSetVariants CollectFields(
            SelectionSetNode selectionSet,
            INamedOutputType type,
            Path path) =>
            _fieldCollector.CollectFields(
                selectionSet, 
                type, 
                path);

        public NameString ResolveTypeName(
            NameString proposedName)
        {
            throw new System.NotImplementedException();
        }

        public NameString ResolveTypeName(
            NameString proposedName,
            ISyntaxNode syntaxNode,
            IReadOnlyList<string>? additionalNamePatterns = null)
        {
            if (_syntaxNodeNames.TryGetValue(syntaxNode, out var takenName))
            {
                return takenName;
            }

            if (_takenNames.Add(proposedName))
            {
                return proposedName;
            }

            for (int i = 1; i < 1000; i++)
            {
                NameString alternativeName = proposedName + "_" + i;

                if (_takenNames.Add(alternativeName))
                {
                    return alternativeName;
                }
            }

            throw new InvalidOperationException(
                "Unable to find a name for the specified syntax node.");
        }
    }
}
