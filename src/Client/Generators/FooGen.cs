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

namespace StrawberryShake.Generators
{
    internal class FooGen
    {
        private readonly Dictionary<IFragment, InterfaceDescriptor> _interfaces =
            new Dictionary<IFragment, InterfaceDescriptor>();
        private readonly HashSet<string> _names = new HashSet<string>();
        private readonly Dictionary<string, ICodeDescriptor> _descriptors =
            new Dictionary<string, ICodeDescriptor>();
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

        public void GenerateField(
            IType parent,
            FieldNode fieldSelection,
            HotChocolate.Path path,
            SelectionSetNode selectionSet)
        {
            INamedType namedType = parent.NamedType();

            var results = new Dictionary<ObjectType, FieldCollectionResult>();

            foreach (ObjectType objectType in
                _schema.GetPossibleTypes(namedType))
            {
                FieldCollectionResult result =
                    _fieldCollector.CollectFields(
                        objectType, selectionSet, path);
                results[objectType] = result;
            }
        }

        private InterfaceDescriptor GenerateUnionTypeModels(
            FieldNode fieldSelection,
            UnionType unionType,
            IReadOnlyDictionary<ObjectType, FieldCollectionResult> results,
            Path path)
        {
            IFragmentNode returnType = null;
            FieldCollectionResult result = results.Values.First();
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
            var modelInterfaces = new List<IInterfaceDescriptor>();

            if (returnType is null)
            {
                string name = CreateName(
                    fieldSelection,
                    unionType,
                    Utilities.NameUtils.GetInterfaceName);
                unionInterface = new InterfaceDescriptor(name, unionType);
                modelInterfaces.AddRange(CreateInterfaces(fragments, path));
            }
            else
            {
                unionInterface = CreateInterface(returnType, path);
                modelInterfaces.AddRange(unionInterface.Implements);
                unionInterface = unionInterface.RemoveAllImplements();
                _interfaces[returnType.Fragment] = unionInterface;
            }

            var models = new List<ClassDescriptor>();

            for (int i = 0; i < modelInterfaces.Count; i++)
            {
                var modelInterface =
                    modelInterfaces[i].TryAddImplements(unionInterface);

                string className = modelInterface.Name.Length > 1
                    ? modelInterface.Name.Substring(1)
                    : modelInterface.Name;
                className = CreateName(className);

                RegisterDescriptor(modelInterface);
                RegisterDescriptor(new ClassDescriptor(
                    modelInterface.Type,
                    className,
                    modelInterface));
            }

            RegisterDescriptor(unionInterface);

            return unionInterface;
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

            var fieldDescriptors = new List<IFieldDescriptor>();

            if (fragmentNode.Fragment.TypeCondition is IComplexOutputType type)
            {
                var fields = new Dictionary<string, FieldSelection>();

                foreach (FieldNode selection in
                    fragmentNode.Fragment.SelectionSet.Selections.OfType<FieldNode>())
                {
                    NameString responseName = selection.Alias == null
                        ? selection.Name.Value
                        : selection.Alias.Value;

                    if (implementedByChildren.Add(responseName))
                    {
                        implementedFields.Add(responseName);
                        FieldCollector.ResolveFieldSelection(
                            type,
                            selection,
                            path,
                            fields);
                    }
                }

                fieldDescriptors.AddRange(fields.Values.Select(t =>
                    new FieldDescriptor(t.Field, t.Selection, t.Field.Type)));
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

        private void RegisterDescriptor(ICodeDescriptor descriptor)
        {
            if (_descriptors.ContainsKey(descriptor.Name))
            {
                _descriptors.Add(descriptor.Name, descriptor);
            }
        }
    }
}
