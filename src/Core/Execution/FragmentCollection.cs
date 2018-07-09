using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class FragmentCollection
    {
        private readonly Dictionary<string, List<Fragment>> _fragments =
            new Dictionary<string, List<Fragment>>();
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

        public IReadOnlyCollection<Fragment> GetFragments(string fragmentName)
        {
            if (fragmentName == null)
            {
                throw new ArgumentNullException(nameof(fragmentName));
            }

            if (!_fragments.TryGetValue(fragmentName,
                out List<Fragment> fragments))
            {
                fragments = new List<Fragment>();
                fragments.AddRange(CreateFragments(fragmentName));
                _fragments[fragmentName] = fragments;
            }

            return fragments;
        }

        private IEnumerable<Fragment> CreateFragments(string fragmentName)
        {
            foreach (FragmentDefinitionNode fragmentDefinition in
                _queryDocument.Definitions
                    .OfType<FragmentDefinitionNode>()
                    .Where(t => t.Name.Value == fragmentName))
            {
                string typeName = fragmentDefinition.TypeCondition.Name.Value;
                if (_schema.TryGetType(typeName, out INamedType type))
                {
                    yield return new Fragment(type, fragmentDefinition.SelectionSet);
                }
            }
        }

        public Fragment GetFragment(InlineFragmentNode inlineFragment)
        {
            if (inlineFragment == null)
            {
                throw new ArgumentNullException(nameof(inlineFragment));
            }

            string fragmentName = CreateInlineFragmentName(inlineFragment);
            if (!_fragments.TryGetValue(fragmentName,
                out List<Fragment> fragments))
            {
                fragments = new List<Fragment>();
                fragments.Add(CreateFragment(inlineFragment));
                _fragments[fragmentName] = fragments;
            }

            return fragments.First();
        }

        private Fragment CreateFragment(InlineFragmentNode inlineFragment)
        {
            // TODO : maybe introduce a tryget to the schema
            INamedType type = _schema.GetType<INamedType>(
                inlineFragment.TypeCondition.Name.Value);
            return new Fragment(type, inlineFragment.SelectionSet);
        }

        private string CreateInlineFragmentName(InlineFragmentNode inlineFragment)
        {
            return $"^__{inlineFragment.Location.Start}_{inlineFragment.Location.End}";
        }
    }
}
