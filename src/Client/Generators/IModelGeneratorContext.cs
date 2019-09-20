using System.Collections.Generic;
using System;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;

namespace StrawberryShake.Generators
{
    internal interface IModelGeneratorContext
    {
        ISchema Schema { get; }

        IQueryDescriptor Query { get; }

        string ClientName { get; }

        string Namespace { get; }

        IReadOnlyCollection<ICodeDescriptor> Descriptors { get; }

        NameString GetOrCreateName(
            ISyntaxNode node,
            NameString name);

        void Register(ICodeDescriptor descriptor);

        PossibleSelections CollectFields(
            INamedOutputType type,
            SelectionSetNode selectionSet,
            Path path);
    }

    internal class ModelGeneratorContext
        : IModelGeneratorContext
    {
        private Dictionary<ISyntaxNode, NameString> _names =
            new Dictionary<ISyntaxNode, NameString>();
        private Dictionary<NameString, ICodeDescriptor> _descriptors =
            new Dictionary<NameString, ICodeDescriptor>();
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

        public PossibleSelections CollectFields(
            INamedOutputType type,
            SelectionSetNode selectionSet,
            Path path) =>
            _collector.CollectFields(type, selectionSet, path);

        public NameString GetOrCreateName(ISyntaxNode node, NameString name)
        {
            if (!_names.TryGetValue(node, out NameString n))
            {
                if (_names.ContainsValue(name))
                {
                    for (int i = 1; i < int.MaxValue; i++)
                    {
                        n = name + i;
                        if (!_names.ContainsValue(n))
                        {
                            break;
                        }
                    }
                }
                else
                {
                    n = name;
                }
                _names.Add(node, n);
            }
            return n;
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
    }
}
