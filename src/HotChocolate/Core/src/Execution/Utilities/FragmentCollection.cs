using System;
using System.Collections.Concurrent;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    internal sealed class FragmentCollection
    {
        private readonly ConcurrentDictionary<object, Fragment?> _fragments =
            new ConcurrentDictionary<object, Fragment?>();
        private readonly ISchema _schema;
        private readonly DocumentNode _document;

        public FragmentCollection(ISchema schema, DocumentNode document)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            _schema = schema;
            _document = document;
        }

        public Fragment? GetFragment(string fragmentName)
        {
            if (fragmentName == null)
            {
                throw new ArgumentNullException(nameof(fragmentName));
            }

            if (!_fragments.TryGetValue(fragmentName, out Fragment? fragment))
            {
                fragment = CreateFragment(fragmentName);
                _fragments[fragmentName] = fragment;
            }

            return fragment;
        }

        private Fragment? CreateFragment(string fragmentName)
        {
            for (int i = 0; i < _document.Definitions.Count; i++)
            {
                if (_document.Definitions[i] is FragmentDefinitionNode fragment &&
                    string.Equals(fragment.Name.Value, fragmentName, StringComparison.Ordinal))
                {
                    if (_schema.TryGetType(fragment.TypeCondition.Name.Value, out INamedType type))
                    {
                        return new Fragment(type, fragment.SelectionSet, fragment.Directives);
                    }
                }
            }

            return null;
        }

        public Fragment? GetFragment(ObjectType parentType, InlineFragmentNode inlineFragment)
        {
            if (!_fragments.TryGetValue(inlineFragment, out Fragment? fragment))
            {
                fragment = CreateFragment(parentType, inlineFragment);
                _fragments[inlineFragment] = fragment;
            }

            return fragment;
        }

        private Fragment CreateFragment(ObjectType parentType, InlineFragmentNode inlineFragment)
        {
            INamedType type = inlineFragment.TypeCondition == null
                ? parentType
                : _schema.GetType<INamedType>(inlineFragment.TypeCondition.Name.Value);

            return new Fragment(type, inlineFragment.SelectionSet, inlineFragment.Directives);
        }
    }
}
