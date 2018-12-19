namespace HotChocolate.Execution
{
    public interface IVariableCollection
    {
        T GetVariable<T>(string variableName);
        bool TryGetVariable<T>(string variableName, out T variableValue);
    }
}
