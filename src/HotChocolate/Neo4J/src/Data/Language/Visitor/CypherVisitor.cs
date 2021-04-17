using System.Collections.Generic;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Data.Neo4J.Language
{
    public partial class CypherVisitor
    {
        private readonly LinkedList<IVisitable> _currentVisitedElements = new ();

        /// <summary>
        /// Target for all rendered parts of the cypher statement.
        /// </summary>
        private readonly CypherWriter _writer = new ();

        /// <summary>
        /// Keeps track of named objects that have been already visited.
        /// </summary>
        private readonly HashSet<INamed> _visitedNamed = new();

        /// <summary>
        /// A set of aliased expressions that already have been seen and for which an alias must be used on each following
        /// appearance.
        /// </summary>
        private readonly HashSet<AliasedExpression> _visitableToAliased = new();

        /// <summary>
        /// Keeps track if currently in an aliased expression so that the content can be skipped when already visited.
        /// </summary>
        private readonly List<AliasedExpression> _currentAliasedElements = new();

        /// <summary>
        /// This keeps track on which level of the tree a separator is needed.
        /// </summary>
        private readonly Dictionary<int, string> _separatorOnLevel = new ();

        /// <summary>
        /// Keeps track of unresolved symbolic names.
        /// </summary>
        private readonly Dictionary<SymbolicName, string> _resolvedSymbolicNames = new ();

        /// <summary>
        /// The current level in the tree of cypher elements.
        /// </summary>
        private int _currentLevel;

        /// <summary>
        /// Will be set to true when entering an already visited node.
        /// </summary>
        private bool _skipNodeContent = false;

        /// <summary>
        /// A flag if we can skip aliasing. This is currently the case in exactly one scenario: A aliased expression passed
        /// to a map project. In that case, the alias is already defined by the key to use in the projected map and we
        /// cannot define him in `AS xxx` fragment.
        /// </summary>
        private bool _skipAliasing;

        public string Print() => _writer.Print();

        private void EnableSeparator(int level, bool on) {
            if (on) {
                _separatorOnLevel[level] = "";
            } else {
                _separatorOnLevel.Remove(level);
            }
        }


        private bool PreEnter(IVisitable visitable)
        {
            _currentAliasedElements.TryPeek(out AliasedExpression lastAliased);
            if (_skipNodeContent || _visitableToAliased.Contains(lastAliased)) {
                return false;
            }

            if (visitable is AliasedExpression aliasedExpression) {
                _currentAliasedElements.Push(aliasedExpression);
            }

            if (visitable is MapProjection) {
                _skipAliasing = true;
            }

            var nextLevel = ++_currentLevel + 1;

            if (visitable is ITypedSubtree)
            {
                EnableSeparator(nextLevel, true);
            }

            if (_separatorOnLevel.ContainsKey(_currentLevel))
            {
                _writer.Write(_separatorOnLevel[_currentLevel]);
            }

            return !_skipNodeContent;
        }

        private void PostLeave(IVisitable visitable)
        {
            if (_separatorOnLevel.ContainsKey(_currentLevel))
            {
                _separatorOnLevel[_currentLevel] = ", ";
            }

            if (visitable is ITypedSubtree)
            {
                EnableSeparator(_currentLevel+1, false);
            }

            --_currentLevel;
        }

        // public string EscapeName(string unescapedName)
        // {
        //     if (unescapedName == null)
        //     {
        //         return string.Empty;
        //     }
        //
        //     MatchCollection matcher = LABEL_AND_TYPE_QUOTATION.Matches(unescapedName);
        //
        //     //return string.Format("`{0}`", Regex.Replace(unescapedName, LABEL_AND_TYPE_QUOTATION.Matches(unescapedName)));
        // }
    }
}
