using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    ///
    /// </summary>
    public partial class CypherVisitor : IVisitor
    {
        private readonly Regex LABEL_AND_TYPE_QUOTATION = new (@"`", RegexOptions.Compiled);

        private Queue<IVisitable> _currentVisitedElements = new ();
        /// <summary>
        /// Target for all rendered parts of the cypher statement.
        /// </summary>
        private readonly CypherWriter _writer = new ();

        /// <summary>
        /// Keeps track of named objects that have been already visited.
        /// </summary>
        private readonly HashSet<Named> _visitedNamed = new();

        /// <summary>
        /// A set of aliased expressions that already have been seen and for which an alias must be used on each following
        /// appearance.
        /// </summary>
        private readonly HashSet<AliasedExpression> _visitableToAliased = new();

        /// <summary>
        /// Keeps track if currently in an aliased expression so that the content can be skipped when already visited.
        /// </summary>
        private readonly Queue<AliasedExpression> _currentAliasedElements = new();

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
        private int _currentLevel = 0;

        /// <summary>
        /// Will be set to true when entering an already visited node.
        /// </summary>
        private bool _skipNodeContent = false;

        /// <summary>
        /// A flag if we can skip aliasing. This is currently the case in exactly one scenario: A aliased expression passed
        /// to a map project. In that case, the alias is already defined by the key to use in the projected map and we
        /// cannot define him in `AS xxx` fragment.
        /// </summary>
        private bool _skipAliasing = false;

        //public CypherQuery Query { get; } = new CypherQuery();
        public string Print() => _writer.Print();

        private void EnableSeparator(int level, bool on) {
            if (on) {
                _separatorOnLevel.Add(level, "");
            } else {
                _separatorOnLevel.Remove(level);
            }
        }

        private string SeparatorOnCurrentLevel() => _separatorOnLevel[_currentLevel];

        protected bool PreEnter(IVisitable visitable)
        {
            // AliasedExpression lastAliased = _currentAliasedElements.Peek();
            // if (_skipNodeContent || _visitableToAliased.Contains(lastAliased)) {
            //     return false;
            // }
            var nextLevel = ++_currentLevel + 1;
            if (visitable is TypedSubtree<Visitable>) {
                EnableSeparator(nextLevel, true);
            }

            return !_skipNodeContent;
        }

        protected void PostLeave(IVisitable visitable)
        {
            //SeparatorOnCurrentLevel().ifPresent(ref -> ref.set(", "));

            if (visitable is TypedSubtree<Visitable>) {
                EnableSeparator(_currentLevel + 1, false);
            }

            // if (Equals(_currentAliasedElements.Peek(), visitable)) {
            //     _currentAliasedElements.Dequeue();
            // }
            //
            // if (visitable is MapProjection) {
            //     _skipAliasing = false;
            // }
            //
            // if (visitable is AliasedExpression aliasedExpression) {
            //     _visitableToAliased.Add(aliasedExpression);
            // }

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
