using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Analyzers.Types;
using StrawberryShake.CodeGeneration.Utilities;
using FieldSelection = StrawberryShake.CodeGeneration.Utilities.FieldSelection;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    public class DocumentAnalyzer
    {
        private UnionTypeSelectionSetAnalyzer _unionTypeSelectionSetAnalyzer =
            new UnionTypeSelectionSetAnalyzer();
        private InterfaceTypeSelectionSetAnalyzer _interfaceTypeSelectionSetAnalyzer =
            new InterfaceTypeSelectionSetAnalyzer();
        private ObjectTypeSelectionSetAnalyzer _objectTypeSelectionSetAnalyzer =
            new ObjectTypeSelectionSetAnalyzer();

        private readonly List<DocumentNode> _documents = new List<DocumentNode>();
        private ISchema? _schema;

        public DocumentAnalyzer SetSchema(ISchema schema)
        {
            return this;
        }

        public DocumentAnalyzer AddDocument(DocumentNode document)
        {
            return this;
        }

        public IDocumentModel Analyze()
        {
            if (_schema is null)
            {
                throw new InvalidOperationException(
                    "You must provide a schema.");
            }

            if (_documents.Count == 0)
            {
                throw new InvalidOperationException(
                    "You must at least provide one document.");
            }

            var context = new DocumentAnalyzerContext(_schema);

            CollectEnumTypes(context, _documents);
            CollectInputObjectTypes(context, _documents);


            throw new NotImplementedException();
        }

        private void CollectOutputTypes(IDocumentAnalyzerContext context, DocumentNode document)
        {
            context.SetDocument(document);

            var backlog = new Queue<FieldSelection>();

            foreach (OperationDefinitionNode operation in
                document.Definitions.OfType<OperationDefinitionNode>())
            {
                var root = Path.New(operation.Name!.Value);

                ObjectType operationType = _schema.GetOperationType(operation.Operation);

                ICodeDescriptor resultType =
                    GenerateOperationSelectionSet(
                        fieldCollector, operationType, operation, root, backlog);

                while (backlog.Any())
                {
                    FieldSelection current = backlog.Dequeue();
                    Path path = current.Path.Append(current.ResponseName);

                    if (!current.Field.Type.NamedType().IsLeafType())
                    {
                        GenerateFieldSelectionSet(
                            context, operation, current.Selection,
                            current.Field.Type, path, backlog);
                    }
                }

                // GenerateResultParserDescriptor(operation, resultType);
            }
        }

        private void GenerateFieldSelectionSet(
            IDocumentAnalyzerContext context,
            OperationDefinitionNode operation,
            FieldNode fieldSelection,
            IType fieldType,
            Path path,
            Queue<FieldSelection> backlog)
        {
            var namedType = (INamedOutputType)fieldType.NamedType();

            PossibleSelections possibleSelections =
                context.CollectFields(
                    namedType,
                    fieldSelection.SelectionSet!,
                    path);

            foreach (SelectionInfo selectionInfo in possibleSelections.Variants)
            {
                EnqueueFields(backlog, selectionInfo.Fields, path);
            }

            if (namedType is UnionType unionType)
            {
                _unionTypeSelectionSetAnalyzer.Analyze(
                    context,
                    operation,
                    fieldSelection,
                    possibleSelections,
                    fieldType,
                    unionType,
                    path);
            }
            else if (namedType is InterfaceType interfaceType)
            {
                _interfaceTypeSelectionSetAnalyzer.Analyze(
                    context,
                    operation,
                    fieldSelection,
                    possibleSelections,
                    fieldType,
                    interfaceType,
                    path);
            }
            else if (namedType is ObjectType objectType)
            {
                _objectTypeSelectionSetAnalyzer.Analyze(
                    context,
                    operation,
                    fieldSelection,
                    possibleSelections,
                    fieldType,
                    objectType,
                    path);
            }
        }

        private static void EnqueueFields(
            Queue<FieldSelection> backlog,
            IEnumerable<FieldSelection> fieldSelections,
            Path path)
        {
            foreach (FieldSelection fieldSelection in fieldSelections)
            {
                backlog.Enqueue(new FieldSelection(
                    fieldSelection.Field,
                    fieldSelection.Selection,
                    path));
            }
        }

        private static void CollectEnumTypes(
            IDocumentAnalyzerContext context,
            IReadOnlyList<DocumentNode> documents)
        {
            var analyzer = new EnumTypeUsageAnalyzer(context.Schema);

            foreach (DocumentNode document in documents)
            {
                analyzer.Analyze(document);
            }

            foreach (EnumType enumType in analyzer.EnumTypes)
            {
                RenameDirective? rename;
                var values = new List<EnumValueModel>();

                foreach (EnumValue enumValue in enumType.Values)
                {
                    rename = enumValue.Directives.SingleOrDefault<RenameDirective>();

                    EnumValueDirective? value =
                        enumValue.Directives.SingleOrDefault<EnumValueDirective>();

                    values.Add(new EnumValueModel(
                        Utilities.NameUtils.GetClassName(rename?.Name ?? enumValue.Name),
                        enumValue,
                        enumValue.Description,
                        value?.Value));
                }

                rename = enumType.Directives.SingleOrDefault<RenameDirective>();

                SerializationTypeDirective? serializationType =
                    enumType.Directives.SingleOrDefault<SerializationTypeDirective>();

                context.Register(new EnumTypeModel(
                    Utilities.NameUtils.GetClassName(rename?.Name ?? enumType.Name),
                    enumType.Description,
                    enumType,
                    serializationType?.Name,
                    values));
            }
        }

        private static void CollectInputObjectTypes(
            IDocumentAnalyzerContext context,
            IReadOnlyList<DocumentNode> documents)
        {
            var analyzer = new InputObjectTypeUsageAnalyzer(context.Schema);

            foreach (DocumentNode document in documents)
            {
                analyzer.Analyze(document);
            }

            foreach (InputObjectType inputObjectType in analyzer.InputObjectTypes)
            {
                RenameDirective? rename;
                var fields = new List<InputFieldModel>();

                foreach (IInputField inputField in inputObjectType.Fields)
                {
                    rename = inputField.Directives.SingleOrDefault<RenameDirective>();

                    fields.Add(new InputFieldModel(
                        Utilities.NameUtils.GetClassName(rename?.Name ?? inputField.Name),
                        inputField.Description,
                        inputField,
                        inputField.Type));
                }

                rename = inputObjectType.Directives.SingleOrDefault<RenameDirective>();

                context.Register(new ComplexInputTypeModel(
                    GetClassName(rename?.Name ?? inputObjectType.Name),
                    inputObjectType.Description,
                    inputObjectType,
                    fields));
            }
        }
    }
}
