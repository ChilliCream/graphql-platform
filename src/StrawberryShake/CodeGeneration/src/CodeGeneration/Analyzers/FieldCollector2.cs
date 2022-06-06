using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using StrawberryShake.CodeGeneration.Analyzers.Models;

namespace StrawberryShake.CodeGeneration.Analyzers;

internal sealed class FieldCollector2
{
    private readonly ISchema _schema;
    private readonly IReadOnlyDictionary<string, FragmentIndexEntry> _fragmentIndex;
    private readonly IDictionary<string, Fragment> _fragmentCache;

    public FieldCollector2(
        ISchema schema,
        IReadOnlyDictionary<string, FragmentIndexEntry> fragmentIndex,
        IDictionary<string, Fragment> fragmentCache)
    {
        _schema = schema;
        _fragmentIndex = fragmentIndex;
        _fragmentCache = fragmentCache;
    }

    public IReadOnlyList<OutputTypeModel> CollectFragments(IDocumentAnalyzerContext context)
    {
        var typeModels = new List<OutputTypeModel>();

        foreach (FragmentIndexEntry entry in _fragmentIndex.Values)
        {
            var fragmentName = entry.Name;
            Path path = Path.New(entry.Name);

            if (!_fragmentCache.TryGetValue(fragmentName, out Fragment? fragment))
            {
                fragment = entry.ToFragment();
                _fragmentCache.Add(fragmentName, fragment);
            }

            SelectionSet selectionSet = CollectFragment(entry, fragment, path);
            var fragmentNode = new FragmentNode(fragment, selectionSet.FragmentNodes);
            typeModels.Add(FragmentHelper.CreateInterface(context, fragmentNode, path));
        }

        return typeModels;
    }

    private SelectionSet CollectFragment(FragmentIndexEntry entry, Fragment fragment, Path path)
    {
        var fields = new OrderedDictionary<string, FieldSelection>();
        var fragmentNodes = new List<FragmentNode>();

        CollectFields(fragment.SelectionSet, entry.TypeCondition, path, fields, fragmentNodes);

        return new SelectionSet(
            entry.TypeCondition,
            fragment.SelectionSet,
            fields.Values.ToArray(),
            fragmentNodes);
    }

    private SelectionSet CollectFieldsInternal(
        SelectionSetNode selectionSetSyntax,
        INamedOutputType type,
        Path path)
    {
        var fields = new OrderedDictionary<string, FieldSelection>();
        var fragmentNodes = new List<FragmentNode>();

        CollectFields(selectionSetSyntax, type, path, fields, fragmentNodes);

        return new SelectionSet(type, selectionSetSyntax, fields.Values.ToArray(), fragmentNodes);
    }

    private void CollectFields(
        SelectionSetNode selectionSetSyntax,
        INamedOutputType selectionSetType,
        Path path,
        IDictionary<string, FieldSelection> fields,
        ICollection<FragmentNode> fragmentNodes)
    {
        foreach (ISelectionNode selection in selectionSetSyntax.Selections)
        {
            ResolveFields(
                selection,
                selectionSetType,
                path,
                fields,
                fragmentNodes);
        }
    }

    private void ResolveFields(
        ISelectionNode selectionSyntax,
        INamedOutputType declaringType,
        Path path,
        IDictionary<string, FieldSelection> fields,
        ICollection<FragmentNode> fragmentNodes)
    {
        if (selectionSyntax is FieldNode field &&
            declaringType is IComplexOutputType complexType)
        {
            ResolveFieldSelection(field, complexType, path, fields);
        }
        else if (selectionSyntax is FragmentSpreadNode fragmentSpread)
        {
            ResolveFragmentSpread(fragmentSpread, declaringType, path, fields, fragmentNodes);
        }
        else if (selectionSyntax is InlineFragmentNode inlineFragment)
        {
            ResolveInlineFragment(inlineFragment, declaringType, path, fields, fragmentNodes);
        }
    }

    private static void ResolveFieldSelection(
        FieldNode fieldSyntax,
        IComplexOutputType declaringType,
        Path path,
        IDictionary<string, FieldSelection> fields)
    {
        NameString fieldName = fieldSyntax.Name.Value;
        NameString responseName = fieldSyntax.Alias?.Value ?? fieldSyntax.Name.Value;
        IOutputField? field = null;

        if (declaringType.Fields.TryGetField(fieldName, out field) ||
            fieldSyntax.Name.Value.EqualsOrdinal(WellKnownNames.TypeName))
        {
            field ??= TypeNameField.Default;

            if (fields.TryGetValue(responseName, out FieldSelection? fieldSelection))
            {
                // if the field has a selection set we will merge it.
                if (fieldSelection.SyntaxNode.SelectionSet is not null)
                {
                    // note: we did not merge any directives. Once we introduce directives for
                    // fields like relay does we will need to revisit this.
                    var selections = fieldSelection.SyntaxNode.SelectionSet.Selections.ToList();
                    selections.AddRange(fieldSyntax.SelectionSet!.Selections);
                    var selectionSet = fieldSyntax.SelectionSet!.WithSelections(selections);
                    fieldSyntax = fieldSelection.SyntaxNode.WithSelectionSet(selectionSet);
                    fieldSelection = new FieldSelection(field, fieldSyntax, fieldSelection.Path);
                    fields[responseName] = fieldSelection;
                }
            }
            else
            {
                fieldSelection = new FieldSelection(
                    field,
                    fieldSyntax,
                    path.Append(responseName));
                fields.Add(responseName, fieldSelection);
            }
        }
        else
        {
            throw new CodeGeneratorException(
                string.Format(Properties.CodeGenerationResources.FieldCollector_FieldDoesNotExist, fieldName, declaringType.Name));
        }
    }

    private void ResolveFragmentSpread(
        FragmentSpreadNode fragmentSpreadSyntax,
        INamedOutputType type,
        Path path,
        IDictionary<string, FieldSelection> fields,
        ICollection<FragmentNode> fragmentNodes)
    {
        var fragmentName = fragmentSpreadSyntax.Name.Value;

        if (!_fragmentIndex.TryGetValue(fragmentName, out FragmentIndexEntry? entry))
        {
            throw ThrowHelper.FragmentNotFound(fragmentName);
        }

        if (!_fragmentCache.TryGetValue(fragmentName, out Fragment? fragment))
        {
            fragment = entry.ToFragment();
            _fragmentCache.Add(fragmentName, fragment);
        }

        if (entry.TypeCondition.IsAssignableFrom(type))
        {
            DirectiveNode? deferDirective = fragmentSpreadSyntax.GetDeferDirective();
            var childFragmentNodes = new List<FragmentNode>();
            var fragmentNode = new FragmentNode(fragment, childFragmentNodes, deferDirective);
            fragmentNodes.Add(fragmentNode);

            CollectFields(fragment.SelectionSet, type, path, fields, childFragmentNodes);
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

        if (fragment.TypeCondition.IsAssignableFrom(type))
        {
            DirectiveNode? deferDirective = inlineFragmentSyntax.GetDeferDirective();
            var childFragmentNodes = new List<FragmentNode>();
            var fragmentNode = new FragmentNode(fragment, childFragmentNodes, deferDirective);
            fragmentNodes.Add(fragmentNode);

            CollectFields(fragment.SelectionSet, type, path, fields, childFragmentNodes);
        }
    }

    private Fragment GetOrCreateInlineFragment(
        InlineFragmentNode inlineFragmentSyntax,
        INamedOutputType parentType)
    {
        var fragmentName = CreateInlineFragmentName(inlineFragmentSyntax);

        if (!_fragmentCache.TryGetValue(fragmentName, out Fragment? fragment))
        {
            fragment = CreateFragment(inlineFragmentSyntax, parentType);
            _fragmentCache.Add(fragmentName, fragment);
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

    private sealed class TypeNameField : IOutputField
    {
        private TypeNameField()
        {
            Name = WellKnownNames.TypeName;
            Type = new NonNullType(new StringType());
            Arguments = FieldCollection<IInputField>.Empty;
        }

        public NameString Name { get; }

        public string? Description => null;

        public IDirectiveCollection Directives => throw new NotImplementedException();

        public ISyntaxNode? SyntaxNode => null;

        public Type RuntimeType => typeof(string);

        public IReadOnlyDictionary<string, object?> ContextData { get; } = new ExtensionData();

        public bool IsIntrospectionField => true;

        public bool IsDeprecated => false;

        public string? DeprecationReason => null;

        public IOutputType Type { get; }

        public IFieldCollection<IInputField> Arguments { get; }

        public IComplexOutputType DeclaringType => throw new NotImplementedException();

        ITypeSystemObject IField.DeclaringType => throw new NotImplementedException();

        public FieldCoordinate Coordinate => throw new NotImplementedException();

        public int Index => 0;

        public static TypeNameField Default { get; } = new();
    }
}
