using System.Collections.Generic;

namespace Zeus.Abstractions
{
    public class QueryDocument
    {

    }

    public interface IQueryDefinition
    {

    }

    public interface ISelection
    {

    }

    public interface ISelectionSet
        : IReadOnlyCollection<ISelection>
    {

    }

    public class OperationDefinition
        : IQueryDefinition
    {
        public string Name { get; }
        public OperationType OperationType { get; }
        public object VariableDefinitions { get; }
        public ISelectionSet SelectionSet { get; }
    }

    public class IFieldSelection
        : ISelection
    {
        public string Name { get; }
        public string Alias { get; }
        public IReadOnlyDictionary<string, Argument> Arguments { get; }
        public object Directives { get; }
        public ISelectionSet SelectionSet { get; }
    }

    public class Argument
    {
        public string Name { get; }
        public IValue Value { get; }
    }


    public enum OperationType
    {
        Query,
        Mutation
    }

}