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
            IReadOnlyList<IFieldDescriptor> fieldDescriptors =
                Array.Empty<IFieldDescriptor>();

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

            var descriptor = new InterfaceDescriptor(
                interfaceName,
                context.Namespace,
                fragmentNode.Fragment.TypeCondition,
                fieldDescriptors,
                implements);
            context.Register(descriptor);
            return descriptor;
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

        protected IFragmentNode HoistFragment(
            INamedType type,
            IFragmentNode fragmentNode)
        {
            (SelectionSetNode s, IReadOnlyList<IFragmentNode> f) current =
                (fragmentNode.Fragment.SelectionSet, fragmentNode.Children);
            IFragmentNode selected = fragmentNode;

            while (!current.s.Selections.OfType<FieldNode>().Any()
                && current.f.Count == 1
                && TypeHelpers.DoesTypeApply(current.f[0].Fragment.TypeCondition, type))
            {
                selected = current.f[0];
                current = (selected.Fragment.SelectionSet, selected.Children);
            }

            return selected;
        }

        protected NameString HoistName(
            INamedType type,
            IFragmentNode fragmentNode)
        {
            if (fragmentNode.Fragment.TypeCondition.Name.Equals(type.Name))
            {
                return fragmentNode.Name;
            }
            else
            {
                foreach (IFragmentNode child in fragmentNode.Children)
                {
                    NameString name = HoistName(type, child);
                    if (name.HasValue)
                    {
                        return name;
                    }
                }

                return default;
            }
        }

        protected static IReadOnlyCollection<SelectionInfo> Normalize(
            PossibleSelections possibleSelections)
        {
            if (possibleSelections.Variants.Count == 0)
            {
                return new List<SelectionInfo> { possibleSelections.ReturnType };
            }
            return possibleSelections.Variants;
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

        protected IReadOnlyList<IFragmentNode> ShedNonMatchingFragments(
            INamedType namedType,
            IFragmentNode fragmentNode)
        {
            var nodes = new List<IFragmentNode>();

            if (fragmentNode.Fragment.TypeCondition.Name.Equals(namedType.Name))
            {
                ShedNonMatchingFragments(namedType, fragmentNode, nodes.Add);
            }
            else
            {
                foreach (IFragmentNode child in fragmentNode.Children)
                {
                    ShedNonMatchingFragments(namedType, child, nodes.Add);
                }
            }

            return nodes;
        }

        private void ShedNonMatchingFragments(
            INamedType namedType,
            IFragmentNode fragmentNode,
            Action<IFragmentNode> add)
        {
            if (fragmentNode.Fragment.TypeCondition.Name.Equals(namedType.Name))
            {
                add(fragmentNode);
            }
            else
            {
                foreach (IFragmentNode child in fragmentNode.Children)
                {
                    ShedNonMatchingFragments(namedType, child, add);
                }
            }
        }
    }
}
