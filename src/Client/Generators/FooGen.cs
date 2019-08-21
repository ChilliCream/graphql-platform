using System.Runtime.CompilerServices;
using System.IO.Pipes;
using System.Xml;
using System.Xml.XPath;
using System.Reflection.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.Generators.Utilities;
using WithDirectives = HotChocolate.Language.IHasDirectives;
using static StrawberryShake.Generators.Utilities.NameUtils;

namespace StrawberryShake.Generators
{
    internal class FooGen
    {
        private readonly Dictionary<IFragment, InterfaceDescriptor> _interfaces =
            new Dictionary<IFragment, InterfaceDescriptor>();
        private readonly HashSet<string> _names = new HashSet<string>();
        private readonly Dictionary<string, ICodeDescriptor> _descriptors =
            new Dictionary<string, ICodeDescriptor>();
        private readonly Dictionary<FieldNode, ICodeDescriptor> _fieldTypes =
            new Dictionary<FieldNode, ICodeDescriptor>();
        private FieldCollector _fieldCollector;
        private readonly ISchema _schema;
        private readonly DocumentNode _document;

        public FooGen(ISchema schema, DocumentNode document)
        {
            _schema = schema;
            _document = document;
            _fieldCollector = new FieldCollector(
                new FragmentCollection(schema, document));
        }

        public void GenerateModels()
        {

        }

        private void GenerateFieldModel(
            IType parent,
            FieldNode fieldSelection,
            Path path,
            Queue<FieldSelection> backlog)
        {
            INamedType namedType = parent.NamedType();

            var typeCases = new Dictionary<ObjectType, FieldCollectionResult>();

            foreach (ObjectType objectType in
                _schema.GetPossibleTypes(namedType))
            {
                FieldCollectionResult result = _fieldCollector.CollectFields(
                    objectType,
                    fieldSelection.SelectionSet,
                    path);

                typeCases[objectType] = result;
            }

            if (namedType is UnionType unionType)
            {
                GenerateUnionTypeModels(
                    fieldSelection,
                    unionType,
                    typeCases,
                    path);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private void GenerateUnionTypeModels(
            FieldNode fieldSelection,
            UnionType unionType,
            IReadOnlyDictionary<ObjectType, FieldCollectionResult> typeCases,
            Path path)
        {
            IFragmentNode returnType = null;
            FieldCollectionResult result = typeCases.Values.First();
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
                unionInterface = new InterfaceDescriptor(name, unionType);
            }
            else
            {
                unionInterface = CreateInterface(returnType, path);
                unionInterface = unionInterface.RemoveAllImplements();
                _interfaces[returnType.Fragment] = unionInterface;
            }

            foreach (var typeCase in typeCases.Select(t => t.Value))
            {
                IFragmentNode fragment = typeCase.Fragments.FirstOrDefault(
                    t => t.Fragment.TypeCondition == typeCase.Type);

                string className;
                string interfaceName;

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
                        typeCase.Type,
                        typeCase.Fields.Select(t =>
                            new FieldDescriptor(
                                t.Field,
                                t.Selection,
                                t.Field.Type))
                            .ToList(),
                        new[] { unionInterface }));
                modelInterfaces.AddRange(CreateInterfaces(typeCase.Fragments, path));

                var modelClass = new ClassDescriptor(
                    className, typeCase.Type, modelInterfaces);

                RegisterDescriptors(modelInterfaces);
                RegisterDescriptor(modelClass);
            }

            RegisterDescriptor(unionInterface);

            _fieldTypes[fieldSelection] = unionInterface;
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
            if (_interfaces.TryGetValue(
                fragmentNode.Fragment,
                out InterfaceDescriptor descriptor))
            {
                return descriptor;
            }

            HashSet<string> implementedFields = levels.Peek();
            var implementedByChildren = new HashSet<string>();
            levels.Push(implementedByChildren);

            var implements = new List<IInterfaceDescriptor>();

            foreach (IFragmentNode child in fragmentNode.Children)
            {
                implements.Add(CreateInterface(child, path, levels));
            }

            levels.Pop();

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
                new FieldDescriptor(t.Field, t.Selection, t.Field.Type))
                .ToList();
        }

        private string CreateName(string name)
        {
            if (!_names.Add(name))
            {
                for (int i = 0; i < int.MaxValue; i++)
                {
                    string n = name + i;
                    if (!_names.Add(n))
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
            if (!TryGetTypeName(withDirectives, out string typeName))
            {
                return CreateName(nameFormatter(typeName));
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

        private void RegisterDescriptors(IEnumerable<ICodeDescriptor> descriptors)
        {
            foreach (ICodeDescriptor descriptor in descriptors)
            {
                RegisterDescriptor(descriptor);
            }
        }

        private void RegisterDescriptor(ICodeDescriptor descriptor)
        {
            if (_descriptors.ContainsKey(descriptor.Name))
            {
                _descriptors.Add(descriptor.Name, descriptor);
            }
        }
    }
}
