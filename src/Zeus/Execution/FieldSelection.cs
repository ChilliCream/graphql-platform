using System.Collections.Generic;
using GraphQLParser.AST;

namespace Zeus.Execution
{
    internal class FieldSelection
    {
        private readonly string _typeName;
        private readonly GraphQLFieldSelection _fieldSelection;

        public FieldSelection(string typeName, GraphQLFieldSelection fieldSelection)
        {
            TypeName = typeName;
            _fieldSelection = fieldSelection;
        }

        public string TypeName { get; }
        public string Name => _fieldSelection.Name.Value;
        public string Alias => _fieldSelection.Alias?.Value;
        public IReadOnlyDictionary<string, object> Arguments { get; }
    }
}