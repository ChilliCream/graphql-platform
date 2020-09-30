using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Execution.Processing;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Processing
{
    public class MatchSelectionsContext : ISyntaxVisitorContext
    {
        private readonly Counter _counter;

        public MatchSelectionsContext(
            ISchema schema,
            IPreparedOperation operation,
            ISelectionSet selectionSet,
            IOutputType type)
        {
            Schema = schema;
            Operation = operation;
            Selections = selectionSet.Selections.ToDictionary(t => t.Field.Name.Value);
            Types = ImmutableStack<IOutputType>.Empty.Push(type);
            _counter = new Counter();
        }

        private MatchSelectionsContext(
            ISchema schema,
            IPreparedOperation operation,
            IReadOnlyDictionary<string, ISelection> selections,
            IImmutableStack<IOutputType> types,
            Counter counter)
        {
            Schema = schema;
            Operation = operation;
            Selections = selections;
            Types = types;
            _counter = counter;
        }

        public ISchema Schema { get; }
        public IPreparedOperation Operation { get; }
        public IReadOnlyDictionary<string, ISelection> Selections { get; }
        public IImmutableStack<IOutputType> Types { get; }
        public int Count { get => _counter.Count; set => _counter.Count = value; }

        public MatchSelectionsContext Branch(IOutputType type, ISelectionSet selectionSet)
        {
            return new MatchSelectionsContext(
                Schema,
                Operation,
                selectionSet.Selections.ToDictionary(t => t.Field.Name.Value),
                Types.Push(type),
                _counter);
        }

        private class Counter
        {
            public int Count { get; set; }
        }
    }
}
