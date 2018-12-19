using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed class FragmentCollection
    {
        private readonly Dictionary<string, Fragment> _fragments =
            new Dictionary<string, Fragment>();
        private readonly ISchema _schema;
        private readonly DocumentNode _queryDocument;

        public FragmentCollection(ISchema schema, DocumentNode queryDocument)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (queryDocument == null)
            {
                throw new ArgumentNullException(nameof(queryDocument));
            }

            _schema = schema;
            _queryDocument = queryDocument;
        }

        public Fragment GetFragment(string fragmentName)
        {
            if (fragmentName == null)
            {
                throw new ArgumentNullException(nameof(fragmentName));
            }

            if (!_fragments.TryGetValue(fragmentName,
                out Fragment fragment))
            {
                fragment = CreateFragment(fragmentName);
                _fragments[fragmentName] = fragment;
            }

            return fragment;
        }

        private Fragment CreateFragment(string fragmentName)
        {
            var fragmentDefinition = _queryDocument.Definitions
                .OfType<FragmentDefinitionNode>()
                .FirstOrDefault(t => string.Equals(
                    t.Name.Value, fragmentName,
                    StringComparison.Ordinal));

            if (fragmentDefinition != null)
            {
                string typeName = fragmentDefinition.TypeCondition.Name.Value;
                if (_schema.TryGetType(typeName, out INamedType type))
                {
                    return new Fragment(type, fragmentDefinition.SelectionSet);
                }
            }

            return null;
        }

        public Fragment GetFragment(
            ObjectType parentType,
            InlineFragmentNode inlineFragment)
        {
            if (parentType == null)
            {
                throw new ArgumentNullException(nameof(parentType));
            }

            if (inlineFragment == null)
            {
                throw new ArgumentNullException(nameof(inlineFragment));
            }

            string fragmentName = CreateInlineFragmentName(inlineFragment);

            if (!_fragments.TryGetValue(fragmentName, out Fragment fragment))
            {
                fragment = CreateFragment(parentType, inlineFragment);
                _fragments[fragmentName] = fragment;

            }

            return fragment;
        }

        private Fragment CreateFragment(
            ObjectType parentType,
            InlineFragmentNode inlineFragment)
        {
            INamedType type;

            if (inlineFragment.TypeCondition == null)
            {
                type = parentType;
            }
            else
            {
                type = _schema.GetType<INamedType>(
                    inlineFragment.TypeCondition.Name.Value);
            }

            return new Fragment(type, inlineFragment.SelectionSet);
        }

        private string CreateInlineFragmentName(
            InlineFragmentNode inlineFragment)
        {
            return $"^__{inlineFragment.Location.Start}_" +
                inlineFragment.Location.End;
        }
    }
}
