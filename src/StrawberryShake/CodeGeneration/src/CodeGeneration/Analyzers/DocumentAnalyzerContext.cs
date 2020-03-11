using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
        private readonly Dictionary<string, ITypeModel> _typeByName =
            new Dictionary<string, ITypeModel>();
        private readonly Dictionary<FieldNode, FieldParserModel> _fieldParsers =
            new Dictionary<FieldNode, FieldParserModel>();
        private readonly Dictionary<OperationDefinitionNode, ParserModel> _resultParsers =
            new Dictionary<OperationDefinitionNode, ParserModel>();
        private FieldCollector? _fieldCollector;

        public DocumentAnalyzerContext(ISchema schema)
        {
            Schema = schema;
        }

        public ISchema Schema { get; }

        public IReadOnlyCollection<ITypeModel> Types => _typeByName.Values;

        public IReadOnlyCollection<FieldParserModel> FieldParsers => _fieldParsers.Values;

        public IReadOnlyCollection<ParserModel> ResultParsers => _resultParsers.Values;

        public IEnumerable<ComplexOutputTypeModel> GetTypes(SelectionSetNode selectionSet)
        {
            if (_types.TryGetValue(selectionSet, out Dictionary<string, ComplexOutputTypeModel>? t))
            {
                return t.Values;
            }
            return Enumerable.Empty<ComplexOutputTypeModel>();
        }

        public bool TryGetModel<T>(string name, [NotNullWhen(true)] out T model)
            where T : class, ITypeModel
        {
            if (_typeByName.TryGetValue(name, out ITypeModel? outputModel)
                && outputModel is T m)
            {
                model = m;
                return true;
            }

            model = default!;
            return false;
        }

        public void SetDocument(DocumentNode document)
        {
            var fragmentCollection = new FragmentCollection(Schema, document);
            _fieldCollector = new FieldCollector(Schema, fragmentCollection);
        }

        public PossibleSelections CollectFields(
            INamedOutputType type,
            SelectionSetNode selectionSet,
            Path path)
        {
            if (_fieldCollector is null)
            {
                throw new InvalidOperationException("The context has no field collector.");
            }
            return _fieldCollector.CollectFields(type, selectionSet, path);
        }

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

        public void Register(ParserModel parser)
        {
            if (!_resultParsers.ContainsKey(parser.Operation))
            {
                _resultParsers.Add(parser.Operation, parser);
            }
            else
            {
                throw new InvalidOperationException(
                    $"A parser for the operation {parser.Operation.Name!.Value} " +
                    "was already registered.");
            }
        }

        public void Register(FieldParserModel parser)
        {
            if (!_fieldParsers.ContainsKey(parser.Selection))
            {
                _fieldParsers.Add(parser.Selection, parser);
            }
            else
            {
                throw new InvalidOperationException(
                    $"A parser for the selection {parser.Path} was already registered.");
            }
        }

        public void Register(ComplexInputTypeModel type)
        {
            if (!_typeByName.ContainsKey(type.Name))
            {
                _usedNames.Add(type.Name);
                _typeByName.Add(type.Name, type);
            }
            else
            {
                throw new InvalidOperationException(
                    $"The type `{type.Name}` was already registered.");
            }
        }

        public void Register(EnumTypeModel type)
        {
            if (!_typeByName.ContainsKey(type.Name))
            {
                _usedNames.Add(type.Name);
                _typeByName.Add(type.Name, type);
            }
            else
            {
                throw new InvalidOperationException(
                    $"The type `{type.Name}` was already registered.");
            }
        }
    }
}
