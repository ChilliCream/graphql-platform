using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        private readonly Dictionary<SelectionSetNode, Dictionary<string, ComplexOutputTypeModel>> _types =
            new Dictionary<SelectionSetNode, Dictionary<string, ComplexOutputTypeModel>>();
        private readonly Dictionary<string, ComplexOutputTypeModel> _typeByName =
            new Dictionary<string, ComplexOutputTypeModel>();
        private readonly Dictionary<FieldNode, FieldParserModel> _parsers =
            new Dictionary<FieldNode, FieldParserModel>();
        private readonly FieldCollector _fieldCollector;

        public DocumentAnalyzerContext(ISchema schema, FieldCollector fieldCollector)
        {
            Schema = schema;
            _fieldCollector = fieldCollector;
        }

        public ISchema Schema { get; }

        public IReadOnlyCollection<ITypeModel> Types => _typeByName.Values;

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

        public void Register(ComplexOutputTypeModel type, bool update = false)
        {
            if (!_types.TryGetValue(type.SelectionSet,
                out Dictionary<string, ComplexOutputTypeModel>? typeByName))
            {
                typeByName = new Dictionary<string, ComplexOutputTypeModel>();
                _types.Add(type.SelectionSet, typeByName);
            }

            if (update || !typeByName.ContainsKey(type.Name))
            {
                typeByName[type.Name] = type;
                _typeByName[type.Name] = type;
            }
            else
            {
                throw new InvalidOperationException(
                    $"The type `{type.Name}` was already registered.");
            }
        }

        public void Register(FieldParserModel parser)
        {
            if (!_parsers.ContainsKey(parser.Selection))
            {
                _parsers.Add(parser.Selection, parser);
            }
            else
            {
                throw new InvalidOperationException(
                    $"A parser for the selection {parser.Path} was already registered.");
            }
        }

        public bool TryGetModel<T>(string name, [NotNullWhen(true)] out T model)
        {
            if (_typeByName.TryGetValue(name, out ComplexOutputTypeModel? outputModel)
                && outputModel is T m)
            {
                model = m;
                return true;
            }

            model = default!;
            return false;
        }

        private class OutputTypeInfo
        {
            public ComplexOutputTypeModel? Model { get; set; }
            public ComplexOutputTypeModel? Interface { get; set; }
        }
    }
}
