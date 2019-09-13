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
        private readonly Dictionary<string, ICodeDescriptor> _descriptors =
            new Dictionary<string, ICodeDescriptor>();
        private readonly Dictionary<OperationDefinitionNode, ICodeDescriptor> _operationTypes =
            new Dictionary<OperationDefinitionNode, ICodeDescriptor>();
        private readonly Dictionary<FieldNode, ICodeDescriptor> _fieldTypes =
            new Dictionary<FieldNode, ICodeDescriptor>();
        private FieldCollector _fieldCollector;
        private readonly ISchema _schema;
        private readonly IQueryDescriptor _query;
        private readonly ISet<string> _usedNames;
        private readonly DocumentNode _document;
        private readonly string _clientName;
        private readonly string _namespace;


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
                new FragmentCollection(schema, query.OriginalDocument));

            Descriptors = Array.Empty<ICodeDescriptor>();
            FieldTypes = new Dictionary<FieldNode, string>();
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
                _descriptors.Values.OfType<IOperationDescriptor>().ToList()));

            FieldTypes = _fieldTypes.ToDictionary(t => t.Key, t => t.Value.Name);
            Descriptors = _descriptors.Values;
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
                _descriptors.Values
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
                IInputClassDescriptor inputClassDescriptor = null;

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
                out IInputClassDescriptor descriptor))
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
            FieldCollectionResult typeCase = _fieldCollector.CollectFields(
                operationType,
                operation.SelectionSet,
                path);

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

            var typeCases = new Dictionary<ObjectType, FieldCollectionResult>();

            foreach (ObjectType objectType in possibleTypes)
            {
                FieldCollectionResult typeCase = _fieldCollector.CollectFields(
                    objectType,
                    fieldSelection.SelectionSet,
                    path);

                EnqueueFields(backlog, typeCase.Fields);

                typeCases[objectType] = typeCase;
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
                _fieldTypes[fieldSelection] = GenerateInterfaceSelectionSet(
                    operation,
                    interfaceType,
                    fieldType,
                    fieldSelection,
                    typeCases.Values,
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
            IReadOnlyCollection<FieldCollectionResult> typeCases,
            Path path)
        {
            IFragmentNode returnType = null;
            FieldCollectionResult result = typeCases.First();
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
                    Utilities.NameUtils.GetInterfaceName);
                unionInterface = new InterfaceDescriptor(
                    name, _namespace, unionType);
            }
            else
            {
                unionInterface = CreateInterface(returnType, path);
                unionInterface = unionInterface.RemoveAllImplements();
            }

            var resultParserTypes = new List<ResultParserTypeDescriptor>();

            foreach (var typeCase in typeCases)
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
                    interfaceName = CreateName(GetInterfaceName(fragment.Fragment.Name));
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

        private ICodeDescriptor GenerateInterfaceSelectionSet(
            OperationDefinitionNode operation,
            InterfaceType interfaceType,
            IType fieldType,
            FieldNode fieldSelection,
            IReadOnlyCollection<FieldCollectionResult> typeCases,
            Path path)
        {
            FieldCollectionResult firstCase = typeCases.First();

            IFragmentNode returnType = HoistFragment(
                interfaceType, firstCase.SelectionSet, firstCase.Fragments);

            InterfaceDescriptor interfaceDescriptor;

            if (returnType is null)
            {
                firstCase = _fieldCollector.CollectFields(
                    interfaceType, firstCase.SelectionSet, path);
                string name = CreateName(fieldSelection, interfaceType, GetClassName);

                var interfaceSelectionSet = new SelectionSetNode(
                    firstCase.Fields.Select(t => t.Selection).ToList());

                returnType = new FragmentNode(new Fragment(
                    name, interfaceType, interfaceSelectionSet));
                _usedNames.Remove(name);
            }

            interfaceDescriptor = CreateInterface(returnType, path);

            var resultParserTypes = new List<ResultParserTypeDescriptor>();

            foreach (FieldCollectionResult typeCase in typeCases)
            {
                GenerateInterfaceTypeCaseModel(
                    typeCase, returnType, resultParserTypes, path);
            }

            RegisterDescriptor(interfaceDescriptor);

            RegisterDescriptor(
                new ResultParserMethodDescriptor(
                    GetPathName(path),
                    operation,
                    fieldType,
                    fieldSelection,
                    path,
                    interfaceDescriptor,
                    resultParserTypes));

            return interfaceDescriptor;
        }

        private void GenerateInterfaceTypeCaseModel(
            FieldCollectionResult typeCase,
            IFragmentNode returnType,
            ICollection<ResultParserTypeDescriptor> resultParser,
            Path path)
        {
            string className;
            IReadOnlyList<IFragmentNode> fragments;

            IFragmentNode modelType = HoistFragment(
                (ObjectType)typeCase.Type,
                typeCase.SelectionSet,
                typeCase.Fragments);

            if (modelType is null)
            {
                fragments = typeCase.Fragments;
                className = CreateName(GetClassName(typeCase.Type.Name));
            }
            else
            {
                fragments = modelType.Children;
                className = CreateName(GetClassName(modelType.Fragment.Name));
            }

            var modelSelectionSet = new SelectionSetNode(
                typeCase.Fields.Select(t => t.Selection).ToList());

            var modelFragment = new FragmentNode(new Fragment(
                className, typeCase.Type, modelSelectionSet));
            modelFragment.Children.AddRange(fragments);
            if (modelFragment.Children.All(t =>
                t.Fragment.SelectionSet != returnType.Fragment.SelectionSet))
            {
                modelFragment.Children.Add(returnType);
            }

            IInterfaceDescriptor modelInterface =
                CreateInterface(modelFragment, path);

            var modelClass = new ClassDescriptor(
                className, _namespace, typeCase.Type, modelInterface);


            RegisterDescriptor(modelInterface);
            RegisterDescriptor(modelClass);

            resultParser.Add(new ResultParserTypeDescriptor(modelClass));
        }

        private ICodeDescriptor GenerateObjectSelectionSet(
            OperationDefinitionNode operation,
            ObjectType objectType,
            IType fieldType,
            WithDirectives fieldOrOperation,
            FieldCollectionResult typeCase,
            Path path)
        {
            IFragmentNode returnType = HoistFragment(
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

        private IFragmentNode HoistFragment(
            INamedType typeContext,
            SelectionSetNode selectionSet,
            IReadOnlyList<IFragmentNode> fragments)
        {
            (SelectionSetNode s, IReadOnlyList<IFragmentNode> f) current =
                (selectionSet, fragments);
            IFragmentNode selected = null;

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

            string typeName = CreateName(
                Utilities.NameUtils.GetInterfaceName(
                    fragmentNode.Fragment.Name));

            return new InterfaceDescriptor(
                typeName,
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
                        return name;
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
            if (TryGetTypeName(withDirectives, out string typeName))
            {
                return CreateName(nameFormatter(typeName));
            }
            else if (withDirectives is OperationDefinitionNode operation)
            {
                return CreateName(nameFormatter(operation.Name.Value));
            }
            return CreateName(nameFormatter(returnType.NamedType().Name));
        }

        private bool TryGetTypeName(
            WithDirectives withDirectives,
            out string typeName)
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
            var queue = new Queue<ICodeDescriptor>();
            queue.Enqueue(descriptor);

            while (queue.Count > 0)
            {
                ICodeDescriptor current = queue.Dequeue();

                if (!_descriptors.ContainsKey(current.Name))
                {
                    _descriptors.Add(current.Name, current);

                    foreach (ICodeDescriptor child in current.GetChildren())
                    {
                        queue.Enqueue(child);
                    }
                }
            }
        }
    }
}
