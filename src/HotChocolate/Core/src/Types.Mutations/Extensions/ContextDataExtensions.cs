namespace HotChocolate.Types;

internal static class ContextDataExtensions
{
    public static List<MutationContextData> GetMutationFields(
        this IDictionary<string, object?> contextData)
    {
        if (contextData.TryGetValue(MutationContextDataKeys.Fields, out var obj) &&
            obj is List<MutationContextData> list)
        {
            return list;
        }

        list = [];
        contextData[MutationContextDataKeys.Fields] = list;
        return list;
    }
}
