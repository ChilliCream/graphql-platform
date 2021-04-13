using System;
using System.Collections.Generic;

namespace StrawberryShake.VisualStudio.Language
{
    // Implements the parsing rules in the Fragments section.
    public ref partial struct StringGraphQLClassifier
    {
        /// <summary>
        /// Parses a fragment spread or inline fragment within a selection set.
        /// <see cref="ParseFragmentSpread" /> and
        /// <see cref="ParseInlineFragment" />.
        /// </summary>
        /// <param name="context">The parser context.</param>

        private void ParseFragment()
        {
            ParseSpread();

            var isOnKeyword = _reader.Value.SequenceEqual(GraphQLKeywords.On);

            if (!isOnKeyword && _reader.Kind == TokenKind.Name)
            {
                ParseFragmentSpread();
            }
            else
            {
                if (isOnKeyword)
                {
                    _classifications.AddClassification(
                        SyntaxClassificationKind.OnKeyword,
                        _reader.Token);
                    MoveNext();
                    ParseNamedType();
                }

                ParseInlineFragment();
            }
        }

        /// <summary>
        /// Parses a fragment definition.
        /// <see cref="FragmentDefinitionNode" />:
        /// fragment FragmentName on TypeCondition Directives? SelectionSet
        /// </summary>
        private void ParseFragmentDefinition()
        {
            ParseFragmentKeyword();
            ParseFragmentName();
            ParseOnKeyword();
            ParseNamedType();
            ParseDirectives(false);
            ParseSelectionSet();
        }

        /// <summary>
        /// Parses a fragment spread.
        /// <see cref="FragmentSpreadNode" />:
        /// ... FragmentName Directives?
        /// </summary>
        private void ParseFragmentSpread()
        {
            ParseFragmentName();
            ParseDirectives(false);
        }

        /// <summary>
        /// Parses an inline fragment.
        /// <see cref="FragmentSpreadNode" />:
        /// ... TypeCondition? Directives? SelectionSet
        /// </summary>
        private void ParseInlineFragment()
        {
            ParseDirectives(false);
            ParseSelectionSet();
        }

        /// <summary>
        /// Parse fragment name.
        /// <see cref="NameNode" />:
        /// Name
        /// </summary>
        private void ParseFragmentName()
        {
            if (_reader.Value.SequenceEqual(GraphQLKeywords.On))
            {
                _classifications.AddClassification(
                    SyntaxClassificationKind.Error,
                    _reader.Token);
                MoveNext();
            }
            else
            {
                ParseName(SyntaxClassificationKind.FragmentReference);
            }
        }
    }
}
