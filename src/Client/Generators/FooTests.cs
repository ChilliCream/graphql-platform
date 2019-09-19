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
    internal abstract class SelectionSetModelGenerator<T>
        where T : INamedType
    {
        public abstract void Generate(
            IModelGeneratorContext context,
            OperationDefinitionNode operation,
            T namedType,
            IType returnType,
            FieldNode fieldSelection,
            PossibleSelections possibleSelections,
            Path path);

        protected IInterfaceDescriptor CreateInterfaceModel(
            IModelGeneratorContext context,
            IFragmentNode fragmentNode,
            Path path)
        {
            var levels = new Stack<ISet<string>>();
            levels.Push(new HashSet<string>());
            return CreateInterfaceModel(
                context,
                fragmentNode,
                path,
                levels);
        }

        private IInterfaceDescriptor CreateInterfaceModel(
            IModelGeneratorContext context,
            IFragmentNode fragmentNode,
            Path path,
            Stack<ISet<string>> levels)
        {
            ISet<string> implementedFields = levels.Peek();
            IReadOnlyList<IFieldDescriptor> fieldDescriptors = Array.Empty<IFieldDescriptor>();

            IReadOnlyList<IInterfaceDescriptor> implements =
                CreateChildInterfaceModels(
                    context,
                    fragmentNode,
                    path,
                    levels,
                    implementedFields);

            if (fragmentNode.Fragment.TypeCondition is IComplexOutputType type)
            {
                fieldDescriptors = CreateFields(
                    type,
                    fragmentNode.Fragment.SelectionSet.Selections,
                    name =>
                    {
                        if (implementedFields.Add(name))
                        {
                            return true;
                        }
                        return false;
                    },
                    path);
            }

            NameString interfaceName = context.GetOrCreateName(
                fragmentNode.Fragment.SelectionSet,
                GetInterfaceName(fragmentNode.Name));

            return new InterfaceDescriptor(
                interfaceName,
                context.Namespace,
                fragmentNode.Fragment.TypeCondition,
                fieldDescriptors,
                implements);
        }

        private IReadOnlyList<IInterfaceDescriptor> CreateChildInterfaceModels(
            IModelGeneratorContext context,
            IFragmentNode fragmentNode,
            Path path,
            Stack<ISet<string>> levels,
            ISet<string> implementedFields)
        {
            var implementedByChildren = new HashSet<string>();
            levels.Push(implementedByChildren);

            var implements = new List<IInterfaceDescriptor>();

            foreach (IFragmentNode child in fragmentNode.Children)
            {
                implements.Add(CreateInterfaceModel(context, child, path, levels));
            }

            levels.Pop();

            foreach (string fieldName in implementedByChildren)
            {
                implementedFields.Add(fieldName);
            }

            return implements;
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

        protected IFragmentNode? HoistFragment(
            INamedType typeContext,
            IFragmentNode fragmentNode)
        {
            (SelectionSetNode s, IReadOnlyList<IFragmentNode> f) current =
                (fragmentNode.Fragment.SelectionSet, fragmentNode.Children);
            IFragmentNode selected = fragmentNode;

            while (!current.s.Selections.OfType<FieldNode>().Any()
                && current.f.Count == 1
                && current.f[0].Fragment.TypeCondition == typeContext)
            {
                selected = current.f[0];
                current = (selected.Fragment.SelectionSet, selected.Children);
            }

            return selected;
        }

        protected static IReadOnlyCollection<SelectionInfo> Normalize(
            IReadOnlyCollection<SelectionInfo> typeCases)
        {
            SelectionInfo first = typeCases.First();
            if (typeCases.Count == 1 || typeCases.All(t =>
                FieldSelectionsAreEqual(t.Fields, first.Fields)))
            {
                return new List<SelectionInfo> { first };
            }
            return typeCases;
        }

        private static bool FieldSelectionsAreEqual(
            IReadOnlyList<FieldSelection> a,
            IReadOnlyList<FieldSelection> b)
        {
            if (a.Count == b.Count)
            {
                for (int i = 0; i < a.Count; i++)
                {
                    if (!ReferenceEquals(a[i].Field, b[i].Field))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        protected static string CreateName(
            IType returnType,
            WithDirectives withDirectives,
            Func<string, string> nameFormatter)
        {
            if (TryGetTypeName(withDirectives, out string? typeName))
            {
                return nameFormatter(typeName!);
            }
            else if (withDirectives is OperationDefinitionNode operation)
            {
                return nameFormatter(operation.Name.Value);
            }
            return nameFormatter(returnType.NamedType().Name);
        }

        protected static bool TryGetTypeName(
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
    }

    internal class InterfaceModelGenerator
        : SelectionSetModelGenerator<InterfaceType>
    {
        public override void Generate(
            IModelGeneratorContext context,
            OperationDefinitionNode operation,
            InterfaceType namedType,
            IType fieldType,
            FieldNode fieldSelection,
            PossibleSelections possibleSelections,
            Path path)
        {
            IFragmentNode returnType = ResolveReturnType(
                context,
                namedType,
                fieldSelection,
                possibleSelections,
                path);

            IInterfaceDescriptor interfaceDescriptor = CreateInterfaceModel(
                context, returnType, path);

            GeneratePossibleTypeModel(
                context,
                operation,
                fieldType,
                fieldSelection,
                possibleSelections,
                returnType,
                interfaceDescriptor,
                path);

            context.Register(interfaceDescriptor);
        }

        private IFragmentNode ResolveReturnType(
            IModelGeneratorContext context,
            InterfaceType namedType,
            FieldNode fieldSelection,
            PossibleSelections possibleSelections,
            Path path)
        {
            string name = null;

            if (possibleSelections.ReturnType.Fragments.Count == 0)
            {
                name = CreateName(namedType, fieldSelection, GetClassName);

            }


            var selectionSet = new SelectionSetNode(
                firstCase.Fields.Select(t => t.Selection).ToList());

            returnType = new FragmentNode(new Fragment(
                name, namedType, selectionSet));


            return returnType;
        }

        private void GeneratePossibleTypeModel(
            IModelGeneratorContext context,
            OperationDefinitionNode operation,
            IType fieldType,
            FieldNode fieldSelection,
            IReadOnlyCollection<SelectionInfo> possibleSelections,
            IFragmentNode returnType,
            IInterfaceDescriptor interfaceDescriptor,
            Path path)
        {
            var resultParserTypes = new List<ResultParserTypeDescriptor>();

            foreach (SelectionInfo possibleSelection in
                Normalize(possibleSelections))
            {
                GeneratePossibleTypeModel(
                    context,
                    possibleSelection,
                    returnType,
                    resultParserTypes,
                    path);
            }

            context.Register(
                new ResultParserMethodDescriptor(
                    GetPathName(path),
                    operation,
                    fieldType,
                    fieldSelection,
                    path,
                    interfaceDescriptor,
                    resultParserTypes));
        }


        private void GeneratePossibleTypeModel(
            IModelGeneratorContext context,
            SelectionInfo typeCase,
            IFragmentNode returnType,
            ICollection<ResultParserTypeDescriptor> resultParser,
            Path path)
        {
            string className;
            IReadOnlyList<IFragmentNode> fragments;

            IFragmentNode? modelType = HoistFragment(
                (ObjectType)typeCase.Type,
                typeCase.SelectionSet,
                typeCase.Fragments);

            if (modelType is null)
            {
                fragments = typeCase.Fragments;
                className = GetClassName(typeCase.Type.Name);
            }
            else
            {
                fragments = modelType.Children;
                className = GetClassName(modelType.Name);
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

    }




    internal interface IModelGeneratorContext
    {
        ISchema Schema { get; }

        IQueryDescriptor Query { get; }

        string ClientName { get; }

        string Namespace { get; }

        NameString GetOrCreateName(
            ISyntaxNode node,
            NameString name);

        void Register(ICodeDescriptor descriptor);

        SelectionInfo CollectFields(
            INamedOutputType type,
            SelectionSetNode selectionSet,
            Path path);
    }
}
