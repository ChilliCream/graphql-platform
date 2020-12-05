using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    internal sealed class ExtractOperationContext : ISyntaxVisitorContext
    {
        private readonly DocumentNode _document;
        private int _index;

        public ExtractOperationContext(DocumentNode document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));

            if (!SelectNext())
            {
                throw new ArgumentException("No operation found!", nameof(document));
            }

            AllFragments = _document.Definitions
                .OfType<FragmentDefinitionNode>()
                .ToDictionary(t => t.Name.Value);
        }

        public OperationDefinitionNode Operation { get; private set; } = default!;

        public List<FragmentDefinitionNode> ExportedFragments { get; } = new();

        public Dictionary<string, FragmentDefinitionNode> AllFragments { get; }

        public HashSet<string> VisitedFragments { get; } = new();

        public bool Next()
        {
            ExportedFragments.Clear();
            VisitedFragments.Clear();
            return SelectNext();
        }

        private bool SelectNext()
        {
            for (var i = _index + 1; i < _document.Definitions.Count; i++)
            {
                if (_document.Definitions[i] is OperationDefinitionNode op)
                {
                    Operation = op;
                    _index = i;
                    return true;
                }
            }

            _index = -1;
            return false;
        }
    }
}
