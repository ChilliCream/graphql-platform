using System.Collections.Immutable;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class FieldResolverTask
    {
        public FieldResolverTask(ImmutableStack<object> source,
            ObjectType objectType, FieldSelection field, Path path)
        {
            Source = source;
            ObjectType = objectType;
            Field = field;
            Path = path;
        }

        public ImmutableStack<object> Source { get; }
        public ObjectType ObjectType { get; }
        public FieldSelection Field { get; }
        public Path Path { get; }
    }
}
