using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HotChocolate.Types;

public static class DataLoaders
{
    [DataLoader]
    public static async Task<IReadOnlyDictionary<int, string>> GetSomeInfoById(
        IReadOnlyList<int> keys)
        => await Task.FromResult(keys.ToDictionary(k => k, k => k + " - some info"));
}
