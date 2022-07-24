using System.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

internal interface IExecutionState
{
    IValueNode GetState(string key, ITypeNode expectedType);

    void AddState(string key, JsonElement value, ITypeNode type);
}
