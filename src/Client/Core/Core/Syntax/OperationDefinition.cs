using System;
using System.Collections.Generic;

namespace HotChocolate.Client.Core.Syntax
{
    public class OperationDefinition : SelectionSet
    {
        public OperationDefinition(OperationType type, string name)
        {
            Type = type;
            Name = name;
            VariableDefinitions = new List<VariableDefinition>();
            FragmentDefinitions = new Dictionary<string, FragmentDefinition>();
        }

        public OperationType Type { get; }
        public string Name { get; }
        public IList<VariableDefinition> VariableDefinitions { get; }
        public Dictionary<string, FragmentDefinition> FragmentDefinitions { get; }
    }
}
