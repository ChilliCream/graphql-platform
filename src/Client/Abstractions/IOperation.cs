using System;
using System.Collections.Generic;

namespace StrawberryShake
{
    public interface IOperation
    {
        string Name { get; }

        IDocument Document { get; }

        Type ResultType { get; }

        IReadOnlyList<VariableValue> GetVariableValues();
    }
}
