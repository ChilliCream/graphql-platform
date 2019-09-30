using System.Linq;
using System.Collections.Generic;
using System;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;

namespace StrawberryShake.Generators
{
    internal class ModelGeneratorContext
        : IModelGeneratorContext
    {
        private Dictionary<ISyntaxNode, ISet<NameString>> _names =
            new Dictionary<ISyntaxNode, ISet<NameString>>();
        private Dictionary<NameString, ICodeDescriptor> _descriptors =
            new Dictionary<NameString, ICodeDescriptor>();
        private Dictionary<FieldNode, ICodeDescriptor> _fieldTypes =
            new Dictionary<FieldNode, ICodeDescriptor>();
        private HashSet<NameString> _usedNames = new HashSet<NameString>();
        private readonly FieldCollector _collector;

        public ModelGeneratorContext(
            ISchema schema,
            IQueryDescriptor query,
            string clientName,
            string ns)
        {
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            Query = query ?? throw new ArgumentNullException(nameof(query));
            ClientName = clientName ?? throw new ArgumentNullException(nameof(clientName));
            Namespace = ns ?? throw new ArgumentNullException(nameof(ns));

            _collector = new FieldCollector(
                schema,
                new FragmentCollection(schema, query.OriginalDocument));
        }

        public ISchema Schema { get; }

        public IQueryDescriptor Query { get; }

        public string ClientName { get; }

        public string Namespace { get; }

        public IReadOnlyCollection<ICodeDescriptor> Descriptors =>
            _descriptors.Values;

        public IReadOnlyDictionary<FieldNode, string> FieldTypes =>
            _fieldTypes.ToDictionary(t => t.Key, t => t.Value.Name);

        public PossibleSelections CollectFields(
            INamedOutputType type,
            SelectionSetNode selectionSet,
            Path path) =>
            _collector.CollectFields(type, selectionSet, path);

        public NameString GetOrCreateName(ISyntaxNode node, NameString name)
        {
            if (!_names.TryGetValue(node, out ISet<NameString>? n))
            {
                n = new HashSet<NameString>();
                _names.Add(node, n);
            }

            if (n.Contains(name))
            {
                return name;
            }

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

            n.Add(current);
            _usedNames.Add(current);
            return current;
        }

        public void Register(ICodeDescriptor descriptor)
        {
            var queue = new Queue<ICodeDescriptor>();
            queue.Enqueue(descriptor);

            while (queue.Count > 0)
            {
                ICodeDescriptor current = queue.Dequeue();

                if (!_descriptors.ContainsKey(current.Name))
                {
                    _descriptors.Add(current.Name, current);

                    foreach (ICodeDescriptor child in current.GetChildren())
                    {
                        queue.Enqueue(child);
                    }
                }
            }
        }

        public void Register(FieldNode field, ICodeDescriptor descriptor)
        {
            if (!_descriptors.TryGetValue(descriptor.Name, out ICodeDescriptor? d))
            {
                d = descriptor;
                Register(d);
            }

            if (!_fieldTypes.ContainsKey(field))
            {
                _fieldTypes.Add(field, descriptor);
            }
        }
    }
}
