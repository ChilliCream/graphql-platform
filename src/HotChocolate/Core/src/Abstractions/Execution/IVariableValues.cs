using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    public interface IVariableValues
    {
        IReadOnlyDictionary<string, object?> ToDictionary(
            IReadOnlyDictionary<string, IInputType> variableDefinitions);
    }
}
