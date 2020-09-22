using System;
using System.Collections.Concurrent;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    internal sealed class FragmentCollection
    {
        private readonly ConcurrentDictionary<object, FragmentInfo?> _fragments =
            new ConcurrentDictionary<object, FragmentInfo?>();
        private readonly ISchema _schema;
        private readonly DocumentNode _document;

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

            if (!_fragments.TryGetValue(fragmentName, out FragmentInfo? fragment))
            {
                fragment = CreateFragment(fragmentName);
                _fragments[fragmentName] = fragment;
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
                        return new FragmentInfo(type, fragment.SelectionSet, fragment.Directives);
                    }
                }
            }

            return null;
        }

        public FragmentInfo? GetFragment(IObjectType parentType, InlineFragmentNode inlineFragment)
        {
            if (!_fragments.TryGetValue(inlineFragment, out FragmentInfo? fragment))
            {
                fragment = CreateFragment(parentType, inlineFragment);
                _fragments[inlineFragment] = fragment;
            }

            return fragment;
        }

        private FragmentInfo CreateFragment(IObjectType parentType, InlineFragmentNode inlineFragment)
        {
            INamedType type = inlineFragment.TypeCondition is null
                ? parentType
                : _schema.GetType<INamedType>(inlineFragment.TypeCondition.Name.Value);

            return new FragmentInfo(type, inlineFragment.SelectionSet, inlineFragment.Directives);
        }
    }
}
