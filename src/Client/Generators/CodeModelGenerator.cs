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

        private InterfaceModelGenerator _interfaceModelGenerator =
            new InterfaceModelGenerator();
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

        private ICodeDescriptor GenerateOperationSelectionSet(
           ObjectType operationType,
           OperationDefinitionNode operation,
           Path path,
           Queue<FieldSelection> backlog)
        {
            SelectionInfo typeCase = _fieldCollector.CollectFields(
                operationType,
                operation.SelectionSet,
                path).ReturnType;

            EnqueueFields(backlog, typeCase.Fields);

            return _operationTypes[operation] =
                GenerateObjectSelectionSet(
                    operation,
                    operationType,
                    operationType,
                    operation,
                    typeCase,
                    path);
        }

        private void GenerateFieldSelectionSet(
            OperationDefinitionNode operation,
            IType fieldType,
            FieldNode fieldSelection,
            Path path,
            Queue<FieldSelection> backlog)
        {
            INamedType namedType = fieldType.NamedType();

            IReadOnlyCollection<ObjectType> possibleTypes =
                namedType is ObjectType ot
                    ? new ObjectType[] { ot }
                    : _schema.GetPossibleTypes(namedType);

            var typeCases = new Dictionary<ObjectType, SelectionInfo>();

            foreach (ObjectType objectType in possibleTypes)
            {
                PossibleSelections possibleSelections =
                    _fieldCollector.CollectFields(
                        objectType,
                        fieldSelection.SelectionSet,
                        path);

                EnqueueFields(backlog, possibleSelections.ReturnType.Fields);

                typeCases[objectType] = possibleSelections.ReturnType;
            }

            if (namedType is UnionType unionType)
            {
                _fieldTypes[fieldSelection] = GenerateUnionSelectionSet(
                    operation,
                    unionType,
                    fieldType,
                    fieldSelection,
                    typeCases.Values,
                    path);
            }
            else if (namedType is InterfaceType interfaceType)
            {
                PossibleSelections possibleSelections =
                    _fieldCollector.CollectFields(
                        interfaceType,
                        fieldSelection.SelectionSet,
                        path);
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
                _fieldTypes[fieldSelection] = GenerateObjectSelectionSet(
                    operation,
                    objectType,
                    fieldType,
                    fieldSelection,
                    typeCases.Values.Single(),
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

        private ICodeDescriptor GenerateUnionSelectionSet(
            OperationDefinitionNode operation,
            UnionType unionType,
            IType fieldType,
            FieldNode fieldSelection,
            IReadOnlyCollection<SelectionInfo> typeCases,
            Path path)
        {
            IFragmentNode? returnType = null;
            SelectionInfo result = typeCases.First();
            IReadOnlyList<IFragmentNode> fragments = result.Fragments;

            while (fragments.Count == 1)
            {
                if (fragments[0].Fragment.TypeCondition == unionType)
                {
                    returnType = fragments[0];
                    fragments = fragments[0].Children;
                }
                else
                {
                    break;
                }
            }

            InterfaceDescriptor unionInterface;

            if (returnType is null)
            {
                string name = CreateName(
                    fieldSelection,
                    unionType,
                    GetInterfaceName);
                unionInterface = new InterfaceDescriptor(
                    name, _namespace, unionType);
            }
            else
            {
                unionInterface = CreateInterface(returnType, path);
                unionInterface = unionInterface.RemoveAllImplements();
            }

            var resultParserTypes = new List<ResultParserTypeDescriptor>();

            foreach (var typeCase in Normalize(typeCases))
            {
                string className;
                string interfaceName;

                IFragmentNode fragment = typeCase.Fragments.FirstOrDefault(
                    t => t.Fragment.TypeCondition == typeCase.Type);

                if (fragment is null)
                {
                    className = CreateName(typeCase.Type.Name);
                    interfaceName = CreateName(GetInterfaceName(className));
                }
                else
                {
                    className = CreateName(fragment.Fragment.Name);
                    interfaceName = GetOrCreateInterfaceName(fragment);
                }

                var modelInterfaces = new List<IInterfaceDescriptor>();
                modelInterfaces.Add(
                    new InterfaceDescriptor(
                        interfaceName,
                        _namespace,
                        typeCase.Type,
                        typeCase.Fields.Select(t =>
                        {
                            string responseName = (t.Selection.Alias ?? t.Selection.Name).Value;
                            return new FieldDescriptor(
                                t.Field,
                                t.Selection,
                                t.Field.Type,
                                t.Path.Append(responseName));
                        }).ToList(),
                        new[] { unionInterface }));
                modelInterfaces.AddRange(CreateInterfaces(typeCase.Fragments, path));

                var modelClass = new ClassDescriptor(
                    className, _namespace, typeCase.Type, modelInterfaces);

                RegisterDescriptors(modelInterfaces);
                RegisterDescriptor(modelClass);

                resultParserTypes.Add(new ResultParserTypeDescriptor(modelClass));
            }

            RegisterDescriptor(unionInterface);

            RegisterDescriptor(
                new ResultParserMethodDescriptor(
                    GetPathName(path),
                    operation,
                    fieldType,
                    fieldSelection,
                    path,
                    unionInterface,
                    resultParserTypes));

            return unionInterface;
        }

        private ICodeDescriptor GenerateObjectSelectionSet(
            OperationDefinitionNode operation,
            ObjectType objectType,
            IType fieldType,
            WithDirectives fieldOrOperation,
            SelectionInfo typeCase,
            Path path)
        {
            IFragmentNode? returnType = HoistFragment(
                objectType, typeCase.SelectionSet, typeCase.Fragments);

            IReadOnlyList<IFragmentNode> fragments;
            string className;

            if (returnType is null)
            {
                fragments = typeCase.Fragments;
                className = CreateName(fieldOrOperation, objectType, GetClassName);
            }
            else
            {
                fragments = returnType.Children;
                className = CreateName(GetClassName(returnType.Fragment.Name));
            }

            var modelSelectionSet = new SelectionSetNode(
                typeCase.Fields.Select(t => t.Selection).ToList());

            var modelFragment = new FragmentNode(new Fragment(
                className, objectType, modelSelectionSet));
            modelFragment.Children.AddRange(fragments);

            IInterfaceDescriptor modelInterface =
                CreateInterface(modelFragment, path);

            var modelClass = new ClassDescriptor(
                className, _namespace, typeCase.Type, modelInterface);

            RegisterDescriptor(modelInterface);
            RegisterDescriptor(modelClass);

            RegisterDescriptor(
                new ResultParserMethodDescriptor(
                    GetPathName(path),
                    operation,
                    fieldType,
                    fieldOrOperation as FieldNode,
                    path,
                    modelInterface,
                    new[] { new ResultParserTypeDescriptor(modelClass) }));

            return modelInterface;
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

        private IReadOnlyList<InterfaceDescriptor> CreateInterfaces(
           IEnumerable<IFragmentNode> fragmentNodes,
           Path path)
        {
            var list = new List<InterfaceDescriptor>();
            foreach (IFragmentNode fragmentNode in fragmentNodes)
            {
                list.Add(CreateInterface(fragmentNode, path));
            }
            return list;
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

        private bool Spread(FieldNode field)
        {
            return field.Directives.Any(t =>
                t.Name.Value.EqualsOrdinal(GeneratorDirectives.Spread));
        }

        private void RegisterDescriptors(IEnumerable<ICodeDescriptor> descriptors)
        {
            foreach (ICodeDescriptor descriptor in descriptors)
            {
                RegisterDescriptor(descriptor);
            }
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

        private static IReadOnlyCollection<SelectionInfo> Normalize(
            IReadOnlyCollection<SelectionInfo> typeCases)
        {
            SelectionInfo first = typeCases.First();
            if (typeCases.Count == 1
                || typeCases.All(t => t.SelectionSet == first.SelectionSet))
            {
                return new List<SelectionInfo> { first };
            }
            return typeCases;
        }
    }


}
