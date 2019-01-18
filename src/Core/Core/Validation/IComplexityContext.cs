using System;
using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    public readonly struct ComplexityContext
    {
        internal ComplexityContext(
            IOutputField fieldDefinition,
            FieldNode fieldSelection,
            ICollection<IOutputField> path,
            IVariableCollection variables,
            CostDirective cost)
        {
            FieldDefinition = fieldDefinition;
            FieldSelection = fieldSelection;
            Path = path;
            Variables = variables;
            Cost = cost;
        }

        public IOutputField FieldDefinition { get; }
        public FieldNode FieldSelection { get; }
        public ICollection<IOutputField> Path { get; }
        public IVariableCollection Variables { get; }
        public CostDirective Cost { get; }
    }
}
