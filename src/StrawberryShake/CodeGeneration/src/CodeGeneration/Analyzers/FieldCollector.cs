using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using IHasDirectives = HotChocolate.Language.IHasDirectives;
using static StrawberryShake.CodeGeneration.Utilities.TypeHelpers;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    internal sealed class FieldCollector
    {
        private readonly Dictionary<string, Fragment> _fragments = new();
        private readonly Cache _cache = new();
        private readonly ISchema _schema;
        private readonly DocumentNode _document;

        public FieldCollector(ISchema schema, DocumentNode document)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
            _document = document ?? throw new ArgumentNullException(nameof(document));
        }

        public SelectionSetVariants CollectFields(
            SelectionSetNode selectionSetSyntax,
            INamedOutputType type,
            Path path)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (selectionSetSyntax is null)
            {
                throw new ArgumentNullException(nameof(selectionSetSyntax));
            }

            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!_cache.TryGetValue(type, out SelectionCache? cache))
            {
                cache = new SelectionCache();
                _cache.Add(type, cache);
            }

            if (!cache.TryGetValue(selectionSetSyntax, out SelectionSetVariants? variants))
            {
                SelectionSet returnType = CollectFieldsInternal(selectionSetSyntax, type, path);

                if (type.IsAbstractType())
                {
                    var list = new List<SelectionSet>();
                    var singleModelShape = true;

                    foreach (ObjectType objectType in _schema.GetPossibleTypes(type))
                    {
                        SelectionSet objectSelection = CollectFieldsInternal(
                            selectionSetSyntax,
                            objectType,
                            path);
                        list.Add(objectSelection);

                        // TODO : do we always want to generate all shapes?
                        // if (!FieldSelectionsAreEqual(returnType.Fields, objectSelection.Fields))
                        {
                            singleModelShape = false;
                        }
                    }

                    if (!singleModelShape)
                    {
                        variants = new SelectionSetVariants(returnType, list);
                    }
                }

                if (variants is null)
                {
                    variants = new SelectionSetVariants(returnType);
                }

                cache.Add(selectionSetSyntax, variants);
            }

            return variants;
        }

        private SelectionSet CollectFieldsInternal(
            SelectionSetNode selectionSetSyntax,
            INamedOutputType type,
            Path path)
        {
            var fields = new OrderedDictionary<string, FieldSelection>();
            var fragmentNodes = new List<FragmentNode>();

            CollectFields(selectionSetSyntax, type, path, fields, fragmentNodes);

            return new SelectionSet(
                type,
                selectionSetSyntax,
                fields.Values.ToList(),
                fragmentNodes);
        }

        private void CollectFields(
            SelectionSetNode selectionSetSyntax,
            INamedOutputType type,
            Path path,
            IDictionary<string, FieldSelection> fields,
            ICollection<FragmentNode> fragmentNodes)
        {
            foreach (ISelectionNode selectionSyntax in selectionSetSyntax.Selections)
            {
                ResolveFields(
                    selectionSyntax,
                    type,
                    path,
                    fields,
                    fragmentNodes);
            }
        }

        private void ResolveFields(
            ISelectionNode selectionSyntax,
            INamedOutputType type,
            Path path,
            IDictionary<string, FieldSelection> fields,
            ICollection<FragmentNode> fragmentNodes)
        {
            if (selectionSyntax is FieldNode fieldSyntax &&
                type is IComplexOutputType complexOutputType)
            {
                ResolveFieldSelection(
                    fieldSyntax,
                    complexOutputType,
                    path,
                    fields);
            }
            else if (selectionSyntax is FragmentSpreadNode fragSpreadSyntax)
            {
                ResolveFragmentSpread(
                    fragSpreadSyntax,
                    type,
                    path,
                    fields,
                    fragmentNodes);
            }
            else if (selectionSyntax is InlineFragmentNode inlineFragSyntax)
            {
                ResolveInlineFragment(
                    inlineFragSyntax,
                    type,
                    path,
                    fields,
                    fragmentNodes);
            }
        }

        internal static void ResolveFieldSelection(
            FieldNode fieldSyntax,
            IComplexOutputType type,
            Path path,
            IDictionary<string, FieldSelection> fields)
        {
            NameString fieldName = fieldSyntax.Name.Value;
            NameString responseName = fieldSyntax.Alias?.Value ?? fieldSyntax.Name.Value;

            if (type.Fields.TryGetField(fieldName, out IOutputField? field))
            {
                if (fields.TryGetValue(responseName, out FieldSelection? fieldSelection))
                {
                    if (fieldSelection.IsConditional && !IsConditional(fieldSyntax))
                    {
                        fieldSelection = new FieldSelection(
                            field,
                            fieldSyntax,
                            path.Append(responseName));
                        fields[responseName] = fieldSelection;
                    }
                }
                else
                {
                    fieldSelection = new FieldSelection(
                        field,
                        fieldSyntax,
                        path.Append(responseName),
                        IsConditional(fieldSyntax));
                    fields.Add(responseName, fieldSelection);
                }
            }
            else if (fieldSyntax.Name.Value is not "__typename")
            {
                // TODO : resources
                throw new CodeGeneratorException(
                    $"Field `{fieldName}` does not exist in type `{type.Name}`.");
            }
        }

        private static bool IsConditional(IHasDirectives hasDirectives) => false;

        private void ResolveFragmentSpread(
            FragmentSpreadNode fragmentSpreadSyntax,
            INamedOutputType type,
            Path path,
            IDictionary<string, FieldSelection> fields,
            ICollection<FragmentNode> fragmentNodes)
        {
            var fragmentName = fragmentSpreadSyntax.Name.Value;

            if (!_fragments.TryGetValue(fragmentName, out Fragment? fragment))
            {
                fragment = CreateFragment(fragmentName);
                _fragments.Add(fragmentName, fragment);
            }

            if (DoesTypeApply(fragment.TypeCondition, type))
            {
                var nodes = new List<FragmentNode>();
                var fragmentNode = new FragmentNode(fragment, nodes);
                fragmentNodes.Add(fragmentNode);

                CollectFields(
                    fragment.SelectionSet,
                    type,
                    path,
                    fields,
                    nodes);
            }
        }

        private void ResolveInlineFragment(
            InlineFragmentNode inlineFragmentSyntax,
            INamedOutputType type,
            Path path,
            IDictionary<string, FieldSelection> fields,
            ICollection<FragmentNode> fragmentNodes)
        {
            Fragment fragment = GetOrCreateInlineFragment(inlineFragmentSyntax, type);

            if (DoesTypeApply(fragment.TypeCondition, type))
            {
                var nodes = new List<FragmentNode>();
                var fragmentNode = new FragmentNode(fragment, nodes);
                fragmentNodes.Add(fragmentNode);

                CollectFields(
                    fragment.SelectionSet,
                    type,
                    path,
                    fields,
                    nodes);
            }
        }

        private static bool FieldSelectionsAreEqual(
            IReadOnlyList<FieldSelection> a,
            IReadOnlyList<FieldSelection> b)
        {
            if (a.Count == b.Count)
            {
                for (var i = 0; i < a.Count; i++)
                {
                    if (!ReferenceEquals(a[i].SyntaxNode, b[i].SyntaxNode))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        private Fragment CreateFragment(string fragmentName)
        {
            FragmentDefinitionNode? fragmentDefinitionSyntax =
                _document.Definitions
                    .OfType<FragmentDefinitionNode>()
                    .FirstOrDefault(t => t.Name.Value.EqualsOrdinal(fragmentName));

            if (fragmentDefinitionSyntax is not null)
            {
                if (_schema.TryGetType(
                    fragmentDefinitionSyntax.TypeCondition.Name.Value,
                    out INamedType type))
                {
                    return new Fragment(
                        fragmentName,
                        FragmentKind.Named,
                        type,
                        fragmentDefinitionSyntax.SelectionSet);
                }
            }

            // TODO : resources
            throw new CodeGeneratorException(
                $"Could not resolve fragment {fragmentName}.");
        }

        private Fragment GetOrCreateInlineFragment(
            InlineFragmentNode inlineFragmentSyntax,
            INamedOutputType parentType)
        {
            string fragmentName = CreateInlineFragmentName(inlineFragmentSyntax);

            if (!_fragments.TryGetValue(fragmentName, out Fragment? fragment))
            {
                fragment = CreateFragment(inlineFragmentSyntax, parentType);
                _fragments[fragmentName] = fragment;
            }

            return fragment;
        }

        private Fragment CreateFragment(
            InlineFragmentNode inlineFragmentSyntax,
            INamedOutputType parentType)
        {
            INamedType type = inlineFragmentSyntax.TypeCondition is null
                ? parentType
                : _schema.GetType<INamedType>(inlineFragmentSyntax.TypeCondition.Name.Value);

            return new Fragment(
                type.Name,
                FragmentKind.Inline,
                type,
                inlineFragmentSyntax.SelectionSet);
        }

        private static string CreateInlineFragmentName(InlineFragmentNode inlineFragmentSyntax) =>
            $"^{inlineFragmentSyntax.Location!.Start}_{inlineFragmentSyntax.Location.End}";

        private class Cache : Dictionary<INamedOutputType, SelectionCache>
        {
        }

        private class SelectionCache : Dictionary<SelectionSetNode, SelectionSetVariants>
        {
        }
    }
}
