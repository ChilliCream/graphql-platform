using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    internal sealed class FragmentCollection
    {
        private readonly ISchema _schema;
        private readonly DocumentNode _document;
        private Dictionary<string, FragmentInfo?>? _fragments;
        private Dictionary<(InlineFragmentNode, string?), FragmentInfo>? _inlineFragments;

        public FragmentCollection(ISchema schema, DocumentNode document)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
            _document = document ?? throw new ArgumentNullException(nameof(document));
        }

        public FragmentInfo? GetFragment(string fragmentName)
        {
            if (fragmentName is null)
            {
                throw new ArgumentNullException(nameof(fragmentName));
            }

            _fragments ??= new Dictionary<string, FragmentInfo?>();

            if (!_fragments.TryGetValue(fragmentName, out FragmentInfo? fragment))
            {
                fragment = CreateFragment(fragmentName);
                _fragments.Add(fragmentName, fragment);
            }

            return fragment;
        }

        private FragmentInfo? CreateFragment(string fragmentName)
        {
            for (var i = 0; i < _document.Definitions.Count; i++)
            {
                if (_document.Definitions[i] is FragmentDefinitionNode fragment &&
                    string.Equals(fragment.Name.Value, fragmentName, StringComparison.Ordinal))
                {
                    if (_schema.TryGetType(fragment.TypeCondition.Name.Value, out INamedType type))
                    {
                        return new FragmentInfo(
                            type, 
                            fragment.SelectionSet, 
                            fragment.Directives,
                            null,
                            fragment);
                    }
                }
            }

            return null;
        }

        public FragmentInfo? GetFragment(IObjectType parentType, InlineFragmentNode inlineFragment)
        {
            if (parentType is null)
            {
                throw new ArgumentNullException(nameof(parentType));
            }

            if (inlineFragment is null)
            {
                throw new ArgumentNullException(nameof(inlineFragment));
            }

            _inlineFragments ??= new Dictionary<(InlineFragmentNode, string?), FragmentInfo>();
            INamedType typeCondition = ResolveTypeCondition(parentType, inlineFragment);
            var key = (inlineFragment, typeCondition.Name.Value);

            if (!_inlineFragments.TryGetValue(key, out FragmentInfo? fragment))
            {
                fragment = new FragmentInfo(
                    typeCondition,
                    inlineFragment.SelectionSet,
                    inlineFragment.Directives,
                    inlineFragment,
                    null);
                _inlineFragments.Add(key, fragment);
            }

            return fragment;
        }

        private INamedType ResolveTypeCondition(
            IObjectType parentType,
            InlineFragmentNode inlineFragment) =>
            inlineFragment.TypeCondition is null
                ? parentType
                : _schema.GetType<INamedType>(inlineFragment.TypeCondition.Name.Value);
    }
}
