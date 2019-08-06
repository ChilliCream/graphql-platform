using System.Collections.Generic;
using HotChocolate.Execution;
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
            IVariableValueCollection variables,
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

        public IVariableValueCollection Variables { get; }

        public CostDirective Cost { get; }
    }
}
