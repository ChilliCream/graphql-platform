using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    public static class FragmentHelper
    {
        public static string? GetReturnTypeName(FieldSelection fieldSelection)
        {
            DirectiveNode? directive =
                fieldSelection.SyntaxNode.Directives.FirstOrDefault(
                    t => t.Name.Value.Equals("returns"));
            if (directive is not null &&
                directive.Arguments.Count == 1 &&
                directive.Arguments[0] is
                {
                    Name: {Value: "fragment"},
                    Value: StringValueNode {Value: {Length: > 0}} sv
                })
            {
                return sv.Value;
            }

            return null;
        }

        public static FragmentNode? GetFragment(FragmentNode fragmentNode, string name)
        {
            if (fragmentNode.Fragment.Kind == FragmentKind.Named &&
                fragmentNode.Fragment.Name.EqualsOrdinal(name))
            {
                return fragmentNode;
            }

            foreach (FragmentNode child in fragmentNode.Nodes)
            {
                if (GetFragment(child, name) is { } n)
                {
                    return n;
                }
            }

            return null;
        }

        public static FragmentNode RewriteForConcreteType(FragmentNode fragmentNode)
        {
            var list = new List<FragmentNode>();

            foreach (var child in fragmentNode.Nodes)
            {
                if (child.Fragment.Kind == FragmentKind.Named)
                {
                    list.Add(RewriteForConcreteType(child));
                }
                else
                {
                    list.Add(child);
                }
            }

            if (fragmentNode.Fragment.TypeCondition.IsInterfaceType())
            {
                Fragment? objectFragment =
                    list
                        .FirstOrDefault(t => t.Fragment.TypeCondition.IsObjectType())?
                        .Fragment;

                if (objectFragment is not null)
                {
                    var fragment = new Fragment(
                        fragmentNode.Fragment.Name + "_" + objectFragment.TypeCondition.Name,
                        fragmentNode.Fragment.Kind,
                        objectFragment.TypeCondition,
                        fragmentNode.Fragment.SelectionSet);

                    return new FragmentNode(fragment, list);
                }
            }

            return new FragmentNode(fragmentNode.Fragment, list);
        }

        public static OutputTypeModel CreateClass(
            IDocumentAnalyzerContext context,
            FragmentNode fragmentNode,
            SelectionSet selectionSet,
            OutputTypeModel @interface)
        {
            NameString name = context.ResolveTypeName(
                GetClassName(fragmentNode.Fragment.Name),
                fragmentNode.Fragment.SelectionSet);

            // We will check for a cached version of this type to have essentially one instance
            // for each type.
            if (context.TryGetModel(name, out OutputTypeModel? typeModel))
            {
                return typeModel;
            }

            var fields = selectionSet.Fields
                .Select(
                    t => new OutputFieldModel(
                        GetPropertyName(t.ResponseName),
                        t.ResponseName,
                        t.Field.Description,
                        t.Field,
                        t.Field.Type,
                        t.SyntaxNode,
                        t.Path))
                .ToList();

            typeModel = new OutputTypeModel(
                name,
                fragmentNode.Fragment.TypeCondition.Description,
                isInterface: false,
                fragmentNode.Fragment.TypeCondition,
                fragmentNode.Fragment.SelectionSet,
                fields,
                new[] {@interface});
            context.RegisterModel(name, typeModel);

            return typeModel;
        }

        public static OutputTypeModel CreateInterface(
            IDocumentAnalyzerContext context,
            FragmentNode fragmentNode,
            Path path,
            IEnumerable<OutputTypeModel>? implements = null)
        {
            var levels = new Stack<ISet<string>>();
            var rootImplements = implements?.ToList() ?? new List<OutputTypeModel>();
            levels.Push(new HashSet<string>());
            return CreateInterface(context, fragmentNode, path, levels, rootImplements);
        }

        private static OutputTypeModel CreateInterface(
            IDocumentAnalyzerContext context,
            FragmentNode fragmentNode,
            Path path,
            Stack<ISet<string>> levels,
            List<OutputTypeModel> rootImplements)
        {
            NameString name = context.ResolveTypeName(
                GetInterfaceName(fragmentNode.Fragment.Name),
                fragmentNode.Fragment.SelectionSet);

            ISet<string> implementedFields = levels.Peek();

            IReadOnlyList<OutputTypeModel> implements =
                CreateImplements(
                    context,
                    fragmentNode,
                    path,
                    levels,
                    implementedFields,
                    rootImplements);

            // We will check for a cached version of this type to have essentially one instance
            // for each type.
            if (context.TryGetModel(name, out OutputTypeModel? typeModel))
            {
                foreach (var model in implements)
                {
                    if (!typeModel.Implements.Contains(model))
                    {
                        rootImplements.Add(model);
                    }
                }

                // important: we mark the fields of this type as resolved for the higher level
                // interfaces.
                AddImplementedFields(typeModel, implementedFields);

                return typeModel;
            }

            // if we are on the first level we need to merge the
            // implements and the root implements.
            if (levels.Count == 1 && rootImplements.Count > 0)
            {
                foreach (var model in implements)
                {
                    if (!rootImplements.Contains(model))
                    {
                        rootImplements.Add(model);
                    }
                }

                implements = rootImplements;
            }

            // mark fields as resolved that come with the implemented types.
            // we mainly do this to include the global implements that are passed into
            // this discovery.
            var implementsCopy = implements.ToList();

            foreach (var model in implements)
            {
                AddImplementedFields(model, implementedFields);

                if (IsImplementedBy(implements, model))
                {
                    implementsCopy.Remove(model);
                }
            }

            implements = implementsCopy;

            typeModel = new OutputTypeModel(
                name,
                fragmentNode.Fragment.TypeCondition.Description,
                isInterface: true,
                fragmentNode.Fragment.TypeCondition,
                fragmentNode.Fragment.SelectionSet,
                CreateFields(fragmentNode, implementedFields, path),
                implements);
            context.RegisterModel(name, typeModel);

            return typeModel;
        }

        private static IReadOnlyList<OutputFieldModel> CreateFields(
            FragmentNode fragmentNode,
            ISet<string> implementedFields,
            Path path)
        {
            // the fragment type is a complex type we will generate a interface with fields.
            if (fragmentNode.Fragment.TypeCondition is INamedOutputType type && 
                type.IsCompositeType())
            {
                var fieldMap = new OrderedDictionary<string, FieldSelection>();
                CollectFields(fragmentNode, type, fieldMap, path);

                if (fieldMap.Count > 0)
                {
                    foreach (var fieldName in fieldMap.Keys.ToArray())
                    {
                        // if we already have implemented this field in a lower level
                        // interface we will just skip it and remove it from the map.
                        if (!implementedFields.Add(fieldName))
                        {
                            fieldMap.Remove(fieldName);
                        }
                    }
                }

                return fieldMap.Values
                    .Select(
                        t => new OutputFieldModel(
                            GetPropertyName(t.ResponseName),
                            t.ResponseName,
                            t.Field.Description,
                            t.Field,
                            t.Field.Type,
                            t.SyntaxNode,
                            t.Path))
                    .ToList();
            }

            return Array.Empty<OutputFieldModel>();
        }

        private static void CollectFields(
            FragmentNode fragmentNode,
            INamedOutputType outputType,
            IDictionary<string, FieldSelection> fields,
            Path path)
        {
            foreach (var inlineFragment in fragmentNode.Nodes.Where(
                t => t.Fragment.Kind == FragmentKind.Inline &&
                     t.Fragment.TypeCondition.IsAssignableFrom(outputType)))
            {
                CollectFields(inlineFragment, outputType, fields, path);
            }

            foreach (FieldNode selection in
                fragmentNode.Fragment.SelectionSet.Selections.OfType<FieldNode>())
            {
                FieldCollector.ResolveFieldSelection(selection, outputType, path, fields);
            }
        }

        private static void AddImplementedFields(
            OutputTypeModel @interface,
            ISet<string> implementedFields)
        {
            foreach (var field in @interface.Fields)
            {
                implementedFields.Add(
                    field.SyntaxNode.Alias?.Value ??
                    field.SyntaxNode.Name.Value);
            }
        }

        private static bool IsImplementedBy(
            IEnumerable<OutputTypeModel> implements,
            OutputTypeModel possibleInterface)
        {
            foreach (var impl in implements)
            {
                if (IsImplementedBy(impl, possibleInterface))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsImplementedBy(
            OutputTypeModel parent,
            OutputTypeModel possibleInterface)
        {
            if (ReferenceEquals(parent, possibleInterface))
            {
                return false;
            }

            if (parent.Implements.Contains(possibleInterface))
            {
                return true;
            }

            foreach (var impl in parent.Implements)
            {
                if (IsImplementedBy(impl, possibleInterface))
                {
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyList<OutputTypeModel> CreateImplements(
            IDocumentAnalyzerContext context,
            FragmentNode parentFragmentNode,
            Path selectionPath,
            Stack<ISet<string>> levels,
            ISet<string> parentFields,
            List<OutputTypeModel> rootImplements)
        {
            // if the parent fragment has no nested fragments we will stop crawling the tree.
            if (parentFragmentNode.Nodes.Count == 0)
            {
                return Array.Empty<OutputTypeModel>();
            }

            var implements = new List<OutputTypeModel>();

            foreach (FragmentNode child in parentFragmentNode.Nodes.Where(
                t => t.Fragment.Kind == FragmentKind.Named))
            {
                // we create a new field level with the fields that are implemented by this level.
                var fields = new HashSet<string>();
                levels.Push(fields);

                implements.Add(
                    CreateInterface(context, child, selectionPath, levels, rootImplements));

                // we add all the fields of this interface to the parent fields level so that we
                // do not create the interface field multiple time on the various levels.
                foreach (string fieldName in fields)
                {
                    parentFields.Add(fieldName);
                }

                // pop level after we have finished creating the interface model.
                levels.Pop();
            }

            return implements;
        }

        public static FragmentNode CreateFragmentNode(
            SelectionSet selectionSet,
            Path path,
            bool appendTypeName = false)
        {
            INamedType namedType = selectionSet.Type.NamedType();
            string name = CreateName(path, GetClassName);

            if (appendTypeName)
            {
                name += "_" + selectionSet.Type.NamedType().Name;
            }

            var returnType = new FragmentNode(
                new Fragment(
                    name,
                    FragmentKind.Structure,
                    namedType,
                    selectionSet.SyntaxNode),
                selectionSet.FragmentNodes);

            return returnType;
        }

        public static FragmentNode CreateFragmentNode(
            INamedType namedType,
            Path selectionPath,
            SelectionSet selectionSet,
            bool appendTypeName = false)
        {
            string name = CreateName(selectionPath, GetClassName);

            if (appendTypeName)
            {
                name += "_" + selectionSet.Type.NamedType().Name;
            }

            return new FragmentNode(
                new Fragment(
                    name,
                    FragmentKind.Structure,
                    namedType,
                    selectionSet.SyntaxNode),
                selectionSet.FragmentNodes);
        }

        public static string CreateName(
            Path selectionPath,
            Func<string, string> nameFormatter)
        {
            var nameBuilder = new StringBuilder();
            Path? current = selectionPath;

            do
            {
                if (current is NamePathSegment name)
                {
                    string part = GetClassName(name.Name);

                    if (nameBuilder.Length > 0)
                    {
                        part += "_";
                    }

                    nameBuilder.Insert(0, part);
                }

                current = current?.Parent;
            } while (current is not null && current != Path.Root);

            return nameFormatter(nameBuilder.ToString());
        }
    }
}
