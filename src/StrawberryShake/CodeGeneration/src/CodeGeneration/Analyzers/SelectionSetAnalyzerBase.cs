using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Types;
using StrawberryShake.CodeGeneration.Utilities;
using WithDirectives = HotChocolate.Language.IHasDirectives;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    internal abstract class SelectionSetAnalyzerBase<T>
        where T : INamedType
    {
        public abstract void Analyze(
           IDocumentAnalyzerContext context,
           OperationDefinitionNode operation,
           FieldNode fieldSelection,
           PossibleSelections possibleSelections,
           IType fieldType,
           T namedType,
           Path path);

        protected ComplexOutputTypeModel CreateInterfaceModel(
            IDocumentAnalyzerContext context,
            IFragmentNode returnTypeFragment,
            Path path)
        {
            var levels = new Stack<ISet<string>>();
            levels.Push(new HashSet<string>());
            return CreateInterfaceModel(context, returnTypeFragment, path, levels);
        }

        private ComplexOutputTypeModel CreateInterfaceModel(
            IDocumentAnalyzerContext context,
            IFragmentNode fragmentNode,
            Path path,
            Stack<ISet<string>> levels)
        {
            NameString name = context.GetOrCreateName(
                fragmentNode.Fragment.SelectionSet,
                GetClassName(fragmentNode.Name));

            ISet<string> implementedFields = levels.Peek();
            IReadOnlyList<OutputFieldModel> fieldModels = Array.Empty<OutputFieldModel>();

            IReadOnlyList<ComplexOutputTypeModel> implements =
                CreateChildInterfaceModels(
                    context,
                    fragmentNode,
                    path,
                    levels,
                    implementedFields);

            if (fragmentNode.Fragment.TypeCondition is IComplexOutputType type)
            {
                fieldModels = CreateFields(
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

            var typeModel = new ComplexOutputTypeModel(
                name,
                fragmentNode.Fragment.TypeCondition.Description,
                true,
                fragmentNode.Fragment.TypeCondition,
                fragmentNode.Fragment.SelectionSet,
                implements,
                fieldModels);

            context.Register(typeModel);

            return typeModel;
        }

        private IReadOnlyList<ComplexOutputTypeModel> CreateChildInterfaceModels(
            IDocumentAnalyzerContext context,
            IFragmentNode fragmentNode,
            Path path,
            Stack<ISet<string>> levels,
            ISet<string> implementedFields)
        {
            if (fragmentNode.Children.Count == 0)
            {
                return Array.Empty<ComplexOutputTypeModel>();
            }

            var implementedByChildren = new HashSet<string>();
            levels.Push(implementedByChildren);

            var implements = new List<ComplexOutputTypeModel>();

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

        private static IReadOnlyList<OutputFieldModel> CreateFields(
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
                return new OutputFieldModel(
                    t.ResponseName,
                    t.Field.Description,
                    t.Field,
                    t.Field.Type,
                    t.Selection,
                    path.Append(responseName));
            }).ToList();
        }

        protected FieldParserModel CreateFieldParserModel(
            IDocumentAnalyzerContext context,
            OperationDefinitionNode operation,
            FieldNode fieldSelection,
            Path path,
            ComplexOutputTypeModel returnType)
        {
            var parserModel = new FieldParserModel(
                operation,
                fieldSelection,
                path,
                returnType,
                new[] { returnType });

            context.Register(parserModel);

            return parserModel;
        }

        protected ComplexOutputTypeModel CreateClassModel(
            IDocumentAnalyzerContext context,
            IFragmentNode returnTypeFragment,
            ComplexOutputTypeModel returnType,
            SelectionInfo selection)
        {
            var fieldNames = new HashSet<string>(
                selection.Fields.Select(t => GetPropertyName(t.ResponseName)));

            string className = context.GetOrCreateName(
                returnTypeFragment.Fragment.SelectionSet,
                GetClassName(returnType.Name),
                fieldNames);

            var modelClass = new ComplexOutputTypeModel(
                className,
                returnTypeFragment.Fragment.TypeCondition.Description,
                false,
                returnTypeFragment.Fragment.TypeCondition,
                returnTypeFragment.Fragment.SelectionSet,
                new[] { returnType },
                Array.Empty<OutputFieldModel>());

            context.Register(modelClass);

            return modelClass;
        }

        protected void CreateClassModels(
            IDocumentAnalyzerContext context,
            IFragmentNode returnTypeFragment,
            ComplexOutputTypeModel returnType,
            FieldNode fieldSelection,
            IReadOnlyCollection<SelectionInfo> selections,
            Path path)
        {
            foreach (SelectionInfo selection in selections)
            {
                IFragmentNode modelType = ResolveReturnType(
                    selection.Type, fieldSelection, selection);

                var interfaces = new List<ComplexOutputTypeModel>();

                foreach (IFragmentNode fragment in ShedNonMatchingFragments(
                    selection.Type, modelType))
                {
                    interfaces.Add(CreateInterfaceModel(context, fragment, path));
                }

                interfaces.Insert(0, returnType);

                NameString typeName = HoistName(selection.Type, modelType);

                if (typeName.IsEmpty)
                {
                    typeName = selection.Type.Name;
                }

                bool update = false;

                var fieldNames = new HashSet<string>(
                    selection.Fields.Select(t => GetPropertyName(t.ResponseName)));

                string className = context.GetOrCreateName(
                    modelType.Fragment.SelectionSet,
                    GetClassName(typeName),
                    fieldNames);

                if (context.TryGetModel(className, out ComplexOutputTypeModel model))
                {
                    var interfaceNames = new HashSet<string>(interfaces.Select(t => t.Name));
                    foreach (ComplexOutputTypeModel type in model.Types.Reverse())
                    {
                        if (interfaceNames.Add(type.Name))
                        {
                            interfaces.Insert(0, type);
                        }
                    }
                    update = true;
                }

                model = new ComplexOutputTypeModel(
                    className,
                    modelType.Fragment.TypeCondition.Description,
                    false,
                    modelType.Fragment.TypeCondition,
                    selection.SelectionSet,
                    interfaces,
                    CreateFields(
                        (IComplexOutputType)modelType.Fragment.TypeCondition,
                        selection.SelectionSet.Selections,
                        n => true,
                        path));

                context.Register(model, update);
                // resultParserTypes.Add(new ResultParserTypeDescriptor(modelClass));
            }
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
                return nameFormatter(operation.Name!.Value);
            }

            INamedType type = returnType.NamedType();

            if (type is HotChocolate.Types.IHasDirectives d)
            {
                IDirective directive = d.Directives[GeneratorDirectives.Name].FirstOrDefault();
                if (directive is { })
                {
                    return nameFormatter(directive.ToObject<NameDirective>().Value);
                }
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

            typeName = directive.Arguments.Single(a =>
                a.Name.Value.EqualsOrdinal("name")).Value.Value as string;
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

        protected IFragmentNode ResolveReturnType(
            INamedType namedType,
            FieldNode fieldSelection,
            SelectionInfo selection)
        {
            var returnType = new FragmentNode(new Fragment(
                CreateName(namedType, fieldSelection, GetClassName),
                FragmentKind.Structure,
                namedType,
                selection.SelectionSet));

            returnType.Children.AddRange(selection.Fragments);

            return HoistFragment(namedType, returnType);
        }
    }
}
