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
        private readonly Dictionary<SelectionSetNode, Dictionary<string, OutputTypeModel>> _types =
            new Dictionary<SelectionSetNode, Dictionary<string, OutputTypeModel>>();
        private readonly Dictionary<string, ITypeModel> _typeByName =
            new Dictionary<string, ITypeModel>();
        private readonly Dictionary<FieldNode, FieldParserModel> _fieldParsers =
            new Dictionary<FieldNode, FieldParserModel>();
        private readonly List<OperationModel> _operations =
            new List<OperationModel>();
        private readonly HashSet<NameString> _usedNames;
        private FieldCollector? _fieldCollector;

        public DocumentAnalyzerContext(ISchema schema)
            : this(schema, Enumerable.Empty<string>())
        {
        }

        public DocumentAnalyzerContext(ISchema schema, IEnumerable<string> reservedNames)
        {
            _usedNames = new HashSet<NameString>(reservedNames.Select(s => new NameString(s)));
            Schema = schema;
        }

        public ISchema Schema { get; }

        public IReadOnlyCollection<ITypeModel> Types => _typeByName.Values;

        public IReadOnlyCollection<OperationModel> Operations => _operations;

        public IReadOnlyCollection<FieldParserModel> FieldParsers => _fieldParsers.Values;

        public IEnumerable<OutputTypeModel> GetTypes(SelectionSetNode selectionSet)
        {
            if (_types.TryGetValue(selectionSet, out Dictionary<string, OutputTypeModel>? t))
            {
                return t.Values;
            }
            return Enumerable.Empty<OutputTypeModel>();
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

        public SelectionVariants CollectFields(
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

        public NameString GetOrCreateName(NameString name)
        {
            var current = name;

            if (_usedNames.Contains(current))
            {
                for (int i = 1; i < int.MaxValue; i++)
                {
                    current = name + i;
                    if (_usedNames.Add(current))
                    {
                        break;
                    }
                }
            }

            _usedNames.Add(current);
            return current;
        }

        public void Register(OutputTypeModel type, bool update = false)
        {
            if (!_types.TryGetValue(type.SelectionSet,
                out Dictionary<string, OutputTypeModel>? typeByName))
            {
                typeByName = new Dictionary<string, OutputTypeModel>();
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

        public void Register(OperationModel operation)
        {
            _operations.Add(operation);
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

        public void Register(InputObjectTypeModel objectType)
        {
            if (!_typeByName.ContainsKey(objectType.Name))
            {
                _usedNames.Add(objectType.Name);
                _typeByName.Add(objectType.Name, objectType);
            }
            else
            {
                throw new InvalidOperationException(
                    $"The type `{objectType.Name}` was already registered.");
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
