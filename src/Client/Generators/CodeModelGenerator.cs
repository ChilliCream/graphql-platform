using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;
using WithDirectives = HotChocolate.Language.IHasDirectives;
using static StrawberryShake.Generators.Utilities.NameUtils;

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
            RegisterDescriptor(_query);

            var backlog = new Queue<FieldSelection>();
            Path root = Path.New("root");

            foreach (var operation in
                _document.Definitions.OfType<OperationDefinitionNode>())
            {
                ObjectType operationType =
                    _schema.GetOperationType(operation.Operation);

                ICodeDescriptor resultDescriptor =
                    GenerateOperationSelectionSet(
                        operationType, operation, root, backlog);

                RegisterDescriptor(GenerateOperation(
                    operationType, operation, resultDescriptor));

                while (backlog.Any())
                {
                    FieldSelection current = backlog.Dequeue();
                    Path path = current.Path.Append(current.ResponseName);

                    GenerateFieldSelectionSet(
                        operation, current.Field.Type,
                        current.Selection, path, backlog);
                }

                RegisterDescriptor(
                    CreateResultParserDescriptor(operation, resultDescriptor));
            }

            RegisterDescriptor(new ClientDescriptor(
                _clientName,
                _namespace,
                _context.Descriptors.OfType<IOperationDescriptor>().ToList()));

            FieldTypes = _fieldTypes.ToDictionary(t => t.Key, t => t.Value.Name);
            Descriptors = _context.Descriptors;
        }

        private IResultParserDescriptor CreateResultParserDescriptor(
            OperationDefinitionNode operation,
            ICodeDescriptor resultDescriptor)
        {
            string name = resultDescriptor is IInterfaceDescriptor
                ? resultDescriptor.Name.Substring(1)
                : resultDescriptor.Name;
            name += "ResultParser";

            return new ResultParserDescriptor
            (
                name,
                _namespace,
                operation,
                resultDescriptor,
                _context.Descriptors
                    .OfType<IResultParserMethodDescriptor>()
                    .Where(t => t.Operation == operation).ToList()
            );
        }

        private ICodeDescriptor GenerateOperation(
            ObjectType operationType,
            OperationDefinitionNode operation,
            ICodeDescriptor resultType)
        {
            var arguments = new List<Descriptors.IArgumentDescriptor>();

            foreach (VariableDefinitionNode variableDefinition in
                operation.VariableDefinitions)
            {
                string typeName = variableDefinition.Type.NamedType().Name.Value;

                if (!_schema.TryGetType(typeName, out INamedType namedType))
                {
                    throw new InvalidOperationException(
                        $"The variable type `{typeName}` is not supported by the schema.");
                }

                IType type = variableDefinition.Type.ToType(namedType);
                IInputClassDescriptor? inputClassDescriptor = null;

                if (namedType is InputObjectType inputObjectType)
                {
                    inputClassDescriptor =
                        GenerateInputObjectType(inputObjectType);
                }

                arguments.Add(new ArgumentDescriptor(
                    variableDefinition.Variable.Name.Value,
                    type,
                    variableDefinition,
                    inputClassDescriptor));
            }

            string operationName =
                CreateName(GetClassName(operation.Name.Value) + "Operation");

            return new OperationDescriptor(
                operationName,
                _namespace,
                operationType,
                operation,
                arguments,
                _query,
                resultType);
        }

        private IInputClassDescriptor GenerateInputObjectType(
            InputObjectType inputObjectType)
        {
            return GenerateInputObjectType(
                inputObjectType,
                new Dictionary<string, IInputClassDescriptor>());
        }

        private IInputClassDescriptor GenerateInputObjectType(
            InputObjectType inputObjectType,
            IDictionary<string, IInputClassDescriptor> knownTypes)
        {
            if (knownTypes.TryGetValue(
                inputObjectType.Name,
                out IInputClassDescriptor? descriptor))
            {
                return descriptor;
            }

            string typeName = CreateName(GetClassName(inputObjectType.Name));

            var fields = new List<Descriptors.IInputFieldDescriptor>();
            descriptor = new InputClassDescriptor(
                typeName, _namespace, inputObjectType, fields);
            knownTypes[inputObjectType.Name] = descriptor;

            foreach (InputField field in inputObjectType.Fields)
            {
                if (field.Type.NamedType() is InputObjectType fieldType)
                {
                    fields.Add(new InputFieldDescriptor(
                        field.Name, field.Type, field,
                        GenerateInputObjectType(fieldType, knownTypes)));
                }
                else
                {
                    fields.Add(new InputFieldDescriptor(
                        field.Name, field.Type, field, null));
                }
            }

            return descriptor;
        }

        private void GenerateOperationSelectionSet(
           ObjectType operationType,
           OperationDefinitionNode operation,
           Path path,
           Queue<FieldSelection> backlog)
        {
            PossibleSelections possibleSelections  =
                _fieldCollector.CollectFields(
                    operationType,
                    operation.SelectionSet,
                    path);

            EnqueueFields(backlog, possibleSelections.ReturnType.Fields);

            _objectModelGenerator.Generate(
                    _context,
                    operation,
                    operationType,
                    operationType,
                    null,
                    possibleSelections,
                    path);
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


        private IFragmentNode? HoistFragment(
            INamedType typeContext,
            SelectionSetNode selectionSet,
            IReadOnlyList<IFragmentNode> fragments)
        {
            (SelectionSetNode s, IReadOnlyList<IFragmentNode> f) current =
                (selectionSet, fragments);
            IFragmentNode? selected = null;

            while (!current.s.Selections.OfType<FieldNode>().Any()
                && current.f.Count == 1
                && current.f[0].Fragment.TypeCondition == typeContext)
            {
                selected = current.f[0];
                current = (selected.Fragment.SelectionSet, selected.Children);
            }

            return selected;
        }

        private InterfaceDescriptor CreateInterface(
            IFragmentNode fragmentNode,
            Path path)
        {
            var levels = new Stack<HashSet<string>>();
            levels.Push(new HashSet<string>());
            return CreateInterface(fragmentNode, path, levels);
        }

        private InterfaceDescriptor CreateInterface(
            IFragmentNode fragmentNode,
            Path path,
            Stack<HashSet<string>> levels)
        {
            HashSet<string> implementedFields = levels.Peek();

            var implementedByChildren = new HashSet<string>();
            levels.Push(implementedByChildren);

            var implements = new List<IInterfaceDescriptor>();

            foreach (IFragmentNode child in fragmentNode.Children)
            {
                implements.Add(CreateInterface(child, path, levels));
            }

            levels.Pop();

            foreach (string fieldName in implementedByChildren)
            {
                implementedFields.Add(fieldName);
            }

            IReadOnlyList<IFieldDescriptor> fieldDescriptors =
                Array.Empty<IFieldDescriptor>();

            if (fragmentNode.Fragment.TypeCondition is IComplexOutputType type)
            {
                fieldDescriptors = CreateFields(
                    type,
                    fragmentNode.Fragment.SelectionSet.Selections,
                    name =>
                    {
                        if (implementedByChildren.Add(name))
                        {
                            implementedFields.Add(name);
                            return true;
                        }
                        return false;
                    },
                    path);
            }

            return new InterfaceDescriptor(
                GetOrCreateInterfaceName(fragmentNode),
                _namespace,
                fragmentNode.Fragment.TypeCondition,
                fieldDescriptors,
                implements);
        }

        private IReadOnlyList<IFieldDescriptor> CreateFields(
            IComplexOutputType type,
            IEnumerable<ISelectionNode> selections,
            Func<string, bool> addField,
            Path path)
        {
            var fields = new Dictionary<string, FieldSelection>();

            foreach (FieldNode selection in selections.OfType<FieldNode>())
            {
                NameString responseName = selection.Alias == null
                    ? selection.Name.Value
                    : selection.Alias.Value;

                if (addField(responseName))
                {
                    FieldCollector.ResolveFieldSelection(
                        type,
                        selection,
                        path,
                        fields);
                }
            }

            return fields.Values.Select(t =>
            {
                string responseName = (t.Selection.Alias ?? t.Selection.Name).Value;
                return new FieldDescriptor(
                    t.Field,
                    t.Selection,
                    t.Field.Type,
                    path.Append(responseName));
            }).ToList();
        }

        private string CreateName(string name)
        {
            if (!_usedNames.Add(name))
            {
                for (int i = 0; i < int.MaxValue; i++)
                {
                    string n = name + i;
                    if (_usedNames.Add(n))
                    {
                        return n;
                    }
                }

                // TODO : resources
                throw new InvalidOperationException(
                    "Could not create a name.");
            }
            return name;
        }

        private string CreateName(
            WithDirectives withDirectives,
            IType returnType,
            Func<string, string> nameFormatter)
        {
            if (TryGetTypeName(withDirectives, out string? typeName))
            {
                return CreateName(nameFormatter(typeName!));
            }
            else if (withDirectives is OperationDefinitionNode operation)
            {
                return CreateName(nameFormatter(operation.Name.Value));
            }
            return CreateName(nameFormatter(returnType.NamedType().Name));
        }

        private bool TryGetTypeName(
            WithDirectives withDirectives,
            out string? typeName)
        {
            DirectiveNode directive =
                withDirectives.Directives.FirstOrDefault(t =>
                    t.Name.Value.EqualsOrdinal(GeneratorDirectives.Type));

            if (directive is null)
            {
                typeName = null;
                return false;
            }

            typeName = (string)directive.Arguments.Single(a =>
                a.Name.Value.EqualsOrdinal("name")).Value.Value;
            return true;
        }

        private void RegisterDescriptor(ICodeDescriptor descriptor)
        {
            _context.Register(descriptor);
        }

        private string GetOrCreateInterfaceName(IFragmentNode fragmentNode)
        {
            if (!_interfaceNames.TryGetValue(
                fragmentNode.Fragment.SelectionSet,
                out string? typeName))
            {
                typeName = CreateName(GetInterfaceName(
                    fragmentNode.Fragment.Name));

                _interfaceNames.Add(
                    fragmentNode.Fragment.SelectionSet,
                    typeName);
            }
            return typeName;
        }
    }
}
