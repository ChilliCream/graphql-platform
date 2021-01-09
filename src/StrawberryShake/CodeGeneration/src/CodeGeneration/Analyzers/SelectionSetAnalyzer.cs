using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models2;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;
using static StrawberryShake.CodeGeneration.Utilities.TypeHelpers;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    internal abstract class SelectionSetAnalyzer
    {
        private readonly StringBuilder _nameBuilder = new StringBuilder();

        public abstract OutputTypeModel Analyze(
           IDocumentAnalyzerContext context,
           FieldSelection fieldSelection,
           SelectionSetVariants selectionSetVariants);


        protected OutputTypeModel CreateInterfaceModel(
            IDocumentAnalyzerContext context,
            FragmentNode returnTypeFragment,
            Path path)
        {
            var levels = new Stack<ISet<string>>();
            levels.Push(new HashSet<string>());
            return CreateInterfaceModel(context, returnTypeFragment, path, levels);
        }

        private OutputTypeModel CreateInterfaceModel(
            IDocumentAnalyzerContext context,
            FragmentNode fragmentNode,
            Path path,
            Stack<ISet<string>> levels)
        {
            NameString name = context.ResolveTypeName(
                GetInterfaceName(fragmentNode.Fragment.Name),
                fragmentNode.Fragment.SelectionSet);

            OutputTypeModel? typeModel;
            // if (context.TryGetModel(name, out OutputTypeModel? typeModel))
            // {
            //     return typeModel;
            // }

            ISet<string> implementedFields = levels.Peek();
            IReadOnlyList<OutputFieldModel> fieldModels = Array.Empty<OutputFieldModel>();

            IReadOnlyList<OutputTypeModel> implements =
                CreateChildInterfaceModels(
                    context,
                    fragmentNode,
                    path,
                    levels,
                    implementedFields);

            if (fragmentNode.Fragment.TypeCondition is IComplexOutputType type)
            {
                fieldModels = CreateFields(
                    fragmentNode.Fragment.SelectionSet.Selections,
                    type,
                    path,
                    name => implementedFields.Add(name));
            }

            typeModel = new OutputTypeModel(
                name,
                fragmentNode.Fragment.TypeCondition.Description,
                true,
                fragmentNode.Fragment.TypeCondition,
                fragmentNode.Fragment.SelectionSet,
                fieldModels,
                implements);

            return typeModel;
        }

        private IReadOnlyList<OutputTypeModel> CreateChildInterfaceModels(
            IDocumentAnalyzerContext context,
            FragmentNode fragmentNode,
            Path path,
            Stack<ISet<string>> levels,
            ISet<string> implementedFields)
        {
            if (fragmentNode.Nodes.Count == 0)
            {
                return Array.Empty<OutputTypeModel>();
            }

            var implementedByChildren = new HashSet<string>();
            levels.Push(implementedByChildren);

            var implements = new List<OutputTypeModel>();

            foreach (FragmentNode child in fragmentNode.Nodes)
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

        protected OutputTypeModel CreateClassModel(
            IDocumentAnalyzerContext context,
            FieldSelection fieldSelection,
            FragmentNode returnTypeFragment,
            OutputTypeModel returnType)
        {
            string className = context.ResolveTypeName(
                GetClassName(returnTypeFragment.Fragment.Name),
                returnTypeFragment.Fragment.SelectionSet);

            var modelClass = new OutputTypeModel(
                className,
                returnTypeFragment.Fragment.TypeCondition.Description,
                false,
                returnTypeFragment.Fragment.TypeCondition,
                returnTypeFragment.Fragment.SelectionSet,
                CreateFields(
                    fieldSelection.SyntaxNode.SelectionSet!.Selections,
                    (IComplexOutputType)returnTypeFragment.Fragment.TypeCondition,
                    fieldSelection.Path),
                new[] { returnType });

            return modelClass;
        }

        private static IReadOnlyList<OutputFieldModel> CreateFields(
            IEnumerable<ISelectionNode> selections,
            IComplexOutputType type,
            Path path,
            Func<string, bool>? addField = null)
        {
            addField ??= _ => true;

            var fields = new Dictionary<string, FieldSelection>();

            foreach (FieldNode selection in selections.OfType<FieldNode>())
            {
                NameString responseName = selection.Alias == null
                    ? selection.Name.Value
                    : selection.Alias.Value;

                if (addField(responseName))
                {
                    FieldCollector.ResolveFieldSelection(
                        selection,
                        type,
                        path,
                        fields);
                }
            }

            return fields.Values.Select(t =>
            {
                return new OutputFieldModel(
                    t.ResponseName,
                    t.Field.Description,
                    t.Field,
                    t.Field.Type,
                    t.SyntaxNode,
                    t.Path);
            }).ToList();
        }

        protected FragmentNode ResolveReturnType(
            FieldSelection fieldSelection,
            SelectionSet selectionSet,
            bool appendTypeName = false)
        {
            INamedType namedType = selectionSet.Type.NamedType();
            string name = CreateName(fieldSelection, GetClassName);

            if (appendTypeName)
            {
                name += "_" + selectionSet.Type.NamedType().Name;
            }

            var returnType = new FragmentNode(new Fragment(
                name,
                FragmentKind.Structure,
                namedType,
                selectionSet.SyntaxNode),
                selectionSet.FragmentNodes);

            return HoistFragment(returnType, namedType);
        }

        protected static FragmentNode HoistFragment(
            FragmentNode fragmentNode,
            INamedType type)
        {
            (SelectionSetNode s, IReadOnlyList<FragmentNode> f) current =
                (fragmentNode.Fragment.SelectionSet, fragmentNode.Nodes);
            FragmentNode selected = fragmentNode;

            while (!current.s.Selections.OfType<FieldNode>().Any()
                && current.f.Count == 1
                && DoesTypeApply(current.f[0].Fragment.TypeCondition, type))
            {
                selected = current.f[0];
                current = (selected.Fragment.SelectionSet, selected.Nodes);
            }

            return selected;
        }

        protected string CreateName(
            FieldSelection fieldSelection,
            Func<string, string> nameFormatter)
        {
            _nameBuilder.Clear();

            Path? current = fieldSelection.Path;

            do
            {
                if (current is NamePathSegment name)
                {
                    string part = GetClassName(name.Name);

                    if (_nameBuilder.Length > 0)
                    {
                        part += "_";
                    }
                    _nameBuilder.Insert(0, part);
                }

                current = current?.Parent;
            }
            while (current is not null && current != Path.Root);

            return _nameBuilder.ToString();
        }
    }
}
