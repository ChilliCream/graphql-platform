using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Integration.DataLoader
{
    public class Query
    {
        public Task<string> GetWithDataLoader(
            string key,
            FieldNode fieldSelection,
            [DataLoader]TestDataLoader testDataLoader)
        {
            return testDataLoader.LoadAsync(key);
        }

        public List<string> GetLoads([DataLoader]TestDataLoader testDataLoader)
        {
            List<string> list = new List<string>();

            foreach (IReadOnlyList<string> request in testDataLoader.Loads)
            {
                list.Add(string.Join(", ", request));
            }

            return list;
        }
    }
}
