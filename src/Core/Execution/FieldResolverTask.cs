using System.Collections.Immutable;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class FieldResolverTask
    {
        public FieldResolverTask(ImmutableStack<object> source,
            ObjectType objectType, FieldSelection fieldSelection,
            Path path, OrderedDictionary result)
        {
            Source = source;
            ObjectType = objectType;
            FieldSelection = fieldSelection;
            FieldType = fieldSelection.Field.Type;
            Path = path;
            Result = result;
        }

        public ImmutableStack<object> Source { get; }
        public ObjectType ObjectType { get; }
        public FieldSelection FieldSelection { get; }
        public IType FieldType { get; }
        public Path Path { get; }
        public OrderedDictionary Result { get; }
        public void SetValue(object value)
        {
            Result[FieldSelection.ResponseName] = value;
        }
    }
}
