using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Utilities
{
    internal sealed class FragmentCollection
    {
        private readonly Dictionary<string, Fragment> _fragments = new();
        private readonly ISchema _schema;
        private readonly DocumentNode _queryDocument;

        public FragmentCollection(ISchema schema, DocumentNode document)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
            _queryDocument = document ?? throw new ArgumentNullException(nameof(document));
        }

        public Fragment GetFragment(string fragmentName)
        {
            if (fragmentName is null)
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

        private Fragment CreateFragment(string fragmentName)
        {
            FragmentDefinitionNode? fragmentDefinition =
                _queryDocument.Definitions
                    .OfType<FragmentDefinitionNode>()
                    .FirstOrDefault(t => string.Equals(
                        t.Name.Value, fragmentName,
                        StringComparison.Ordinal));

            if (fragmentDefinition is not null)
            {
                string typeName = fragmentDefinition.TypeCondition.Name.Value;
                if (_schema.TryGetType(typeName, out INamedType type))
                {
                    return new Fragment(
                        fragmentName,
                        FragmentKind.Named,
                        type,
                        fragmentDefinition.SelectionSet);
                }
            }

            throw new InvalidOperationException(
                $"Could not resolve fragment {fragmentName}.");
        }

        public Fragment GetFragment(
            INamedOutputType parentType,
            InlineFragmentNode inlineFragment)
        {
            if (parentType is null)
            {
                throw new ArgumentNullException(nameof(parentType));
            }

            if (inlineFragment is null)
            {
                throw new ArgumentNullException(nameof(inlineFragment));
            }

            string fragmentName = CreateInlineFragmentName(inlineFragment);

            if (!_fragments.TryGetValue(fragmentName, out Fragment? fragment))
            {
                fragment = CreateFragment(parentType, inlineFragment);
                _fragments[fragmentName] = fragment;
            }

            return fragment;
        }

        private Fragment CreateFragment(
            INamedOutputType parentType,
            InlineFragmentNode inlineFragment)
        {
            INamedType type = inlineFragment.TypeCondition is null
                ? parentType
                : _schema.GetType<INamedType>(inlineFragment.TypeCondition.Name.Value);

            return new Fragment(type.Name, FragmentKind.Inline, type, inlineFragment.SelectionSet);
        }

        private static string CreateInlineFragmentName(
            InlineFragmentNode inlineFragment) =>
            $"^{inlineFragment.Location!.Start}_{inlineFragment.Location.End}";
    }
}
