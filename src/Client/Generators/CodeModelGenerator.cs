using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;

namespace StrawberryShake.Generators
{
    internal class CodeModelGenerator
    {
        private readonly Dictionary<OperationDefinitionNode, ICodeDescriptor> _operationTypes =
            new Dictionary<OperationDefinitionNode, ICodeDescriptor>();
        private readonly Dictionary<FieldNode, ICodeDescriptor> _fieldTypes =
            new Dictionary<FieldNode, ICodeDescriptor>();
        private readonly Dictionary<ISyntaxNode, string> _interfaceNames =
            new Dictionary<ISyntaxNode, string>();
        private FieldCollector _fieldCollector;
        private readonly ISchema _schema;
        private readonly IQueryDescriptor _query;
        private readonly ISet<string> _usedNames;
        private readonly DocumentNode _document;
        private readonly string _clientName;
        private readonly string _namespace;

        private OperationModelGenerator _operationModelGenerator =
            new OperationModelGenerator();
        private ObjectModelGenerator _objectModelGenerator =
            new ObjectModelGenerator();
        private InterfaceModelGenerator _interfaceModelGenerator =
            new InterfaceModelGenerator();
        private UnionModelGenerator _unionModelGenerator =
            new UnionModelGenerator();
        private IModelGeneratorContext _context;

        public CodeModelGenerator(
            ISchema schema,
            IQueryDescriptor query,
            ISet<string> usedNames,
            string clientName,
            string ns)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
            _query = query ?? throw new ArgumentNullException(nameof(query));
            _usedNames = usedNames ?? throw new ArgumentNullException(nameof(usedNames));
            _clientName = clientName ?? throw new ArgumentNullException(nameof(clientName));
            _namespace = ns ?? throw new ArgumentNullException(nameof(ns));

            _document = query.OriginalDocument;
            _fieldCollector = new FieldCollector(
                schema,
                new FragmentCollection(schema, query.OriginalDocument));

            Descriptors = Array.Empty<ICodeDescriptor>();
            FieldTypes = new Dictionary<FieldNode, string>();

            _context = new ModelGeneratorContext(schema, query, clientName, ns);
        }

        public IReadOnlyCollection<ICodeDescriptor> Descriptors { get; private set; }

        public IReadOnlyDictionary<FieldNode, string> FieldTypes { get; private set; }

        public void Generate()
        {
            _context.Register(_query);

            var backlog = new Queue<FieldSelection>();
            Path root = Path.New("root");

            foreach (var operation in
                _document.Definitions.OfType<OperationDefinitionNode>())
            {
                ObjectType operationType =
                    _schema.GetOperationType(operation.Operation);

                ICodeDescriptor resultType =
                    GenerateOperationSelectionSet(
                        operationType, operation, root, backlog);

                while (backlog.Any())
                {
                    FieldSelection current = backlog.Dequeue();
                    Path path = current.Path.Append(current.ResponseName);
                    if (!current.Field.Type.IsLeafType())
                    {
                        GenerateFieldSelectionSet(
                            operation, current.Field.Type,
                            current.Selection, path, backlog);
                    }
                }

                GenerateResultParserDescriptor(operation, resultType);
            }

            _context.Register(new ClientDescriptor(
                _context.ClientName,
                _namespace,
                _context.Descriptors.OfType<IOperationDescriptor>().ToList()));

            FieldTypes = _context.FieldTypes;
            Descriptors = _context.Descriptors;
        }

        private void GenerateResultParserDescriptor(
            OperationDefinitionNode operation,
            ICodeDescriptor resultDescriptor)
        {
            string name = resultDescriptor is IInterfaceDescriptor
                ? resultDescriptor.Name.Substring(1)
                : resultDescriptor.Name;
            name += "ResultParser";

            _context.Register(new ResultParserDescriptor
            (
                name,
                _context.Namespace,
                operation,
                resultDescriptor,
                _context.Descriptors
                    .OfType<IResultParserMethodDescriptor>()
                    .Where(t => t.Operation == operation).ToList()
            ));
        }

        private ICodeDescriptor GenerateOperationSelectionSet(
           ObjectType operationType,
           OperationDefinitionNode operation,
           Path path,
           Queue<FieldSelection> backlog)
        {
            PossibleSelections possibleSelections =
                _fieldCollector.CollectFields(
                    operationType,
                    operation.SelectionSet,
                    path);

            EnqueueFields(backlog, possibleSelections.ReturnType.Fields);

            ICodeDescriptor resultDescriptor = _objectModelGenerator.Generate(
                    _context,
                    operation,
                    operationType,
                    new NonNullType(operationType),
                    new FieldNode(
                        null,
                        new NameNode(operation.Name.Value),
                        null,
                        new[]
                        {
                            new DirectiveNode(
                                GeneratorDirectives.Type,
                                new ArgumentNode("name", operation.Name.Value)),
                            new DirectiveNode(GeneratorDirectives.Operation)
                        },
                        Array.Empty<ArgumentNode>(),
                        null),
                    possibleSelections,
                    path);

            _operationModelGenerator.Generate(
                _context,
                operationType,
                operation,
                resultDescriptor);

            return resultDescriptor;
        }

        private void GenerateFieldSelectionSet(
            OperationDefinitionNode operation,
            IType fieldType,
            FieldNode fieldSelection,
            Path path,
            Queue<FieldSelection> backlog)
        {
            var namedType = (INamedOutputType)fieldType.NamedType();

            PossibleSelections possibleSelections =
                _fieldCollector.CollectFields(
                    namedType,
                    fieldSelection.SelectionSet,
                    path);

            foreach (SelectionInfo selectionInfo in possibleSelections.Variants)
            {
                EnqueueFields(backlog, selectionInfo.Fields);
            }

            if (namedType is UnionType unionType)
            {
                _unionModelGenerator.Generate(
                    _context,
                    operation,
                    unionType,
                    fieldType,
                    fieldSelection,
                    possibleSelections,
                    path);
            }
            else if (namedType is InterfaceType interfaceType)
            {
                _interfaceModelGenerator.Generate(
                    _context,
                    operation,
                    interfaceType,
                    fieldType,
                    fieldSelection,
                    possibleSelections,
                    path);
            }
            else if (namedType is ObjectType objectType)
            {
                _objectModelGenerator.Generate(
                    _context,
                    operation,
                    objectType,
                    fieldType,
                    fieldSelection,
                    possibleSelections,
                    path);
            }
        }

        private static void EnqueueFields(
            Queue<FieldSelection> backlog,
            IEnumerable<FieldSelection> fieldSelections)
        {
            foreach (FieldSelection fieldSelection in fieldSelections)
            {
                backlog.Enqueue(fieldSelection);
            }
        }
    }
}
