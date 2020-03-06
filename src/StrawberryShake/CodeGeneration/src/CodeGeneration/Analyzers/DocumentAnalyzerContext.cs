using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Utilities;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    internal sealed class DocumentAnalyzerContext
        : IDocumentAnalyzerContext
    {
        private readonly Dictionary<ISyntaxNode, ISet<NameString>> _names =
            new Dictionary<ISyntaxNode, ISet<NameString>>();
        private readonly HashSet<NameString> _usedNames = new HashSet<NameString>();
        private readonly Dictionary<SelectionSetNode, ComplexOutputTypeModel> _types =
            new Dictionary<SelectionSetNode, ComplexOutputTypeModel>();
        private readonly Dictionary<FieldNode, FieldParserModel> _parsers =
            new Dictionary<FieldNode, FieldParserModel>();
        private readonly FieldCollector _fieldCollector;

        public DocumentAnalyzerContext(ISchema schema, FieldCollector fieldCollector)
        {
            Schema = schema;
            _fieldCollector = fieldCollector;
        }

        public ISchema Schema { get; }

        public IReadOnlyCollection<ITypeModel> Types => _types.Values;

        public PossibleSelections CollectFields(
            INamedOutputType type,
            SelectionSetNode selectionSet,
            Path path) =>
            _fieldCollector.CollectFields(type, selectionSet, path);

        public NameString GetOrCreateName(
            ISyntaxNode node,
            NameString name,
            ISet<string>? skipNames = null)
        {
            if (!_names.TryGetValue(node, out ISet<NameString>? n))
            {
                n = new HashSet<NameString>();
                _names.Add(node, n);
            }

            if ((skipNames is null || !skipNames.Contains(name))
                && n.Contains(name))
            {
                return name;
            }

            var current = name;

            if ((skipNames is { } && skipNames.Contains(name))
                || _usedNames.Contains(current))
            {
                for (int i = 1; i < int.MaxValue; i++)
                {
                    current = name + i;
                    if ((skipNames is null || !skipNames.Contains(current))
                        && _usedNames.Add(current))
                    {
                        break;
                    }
                }
            }

            n.Add(current);
            _usedNames.Add(current);
            return current;
        }

        public void Register(ComplexOutputTypeModel type)
        {
            if (!_types.ContainsKey(type.SelectionSet))
            {
                _types.Add(type.SelectionSet, type);
            }
        }

        public void Register(FieldParserModel parser)
        {
            if (!_parsers.ContainsKey(parser.Selection))
            {
                _parsers.Add(parser.Selection, parser);
            }
        }
    }
}
