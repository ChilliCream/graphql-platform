using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    internal interface IDataLoaderCollection
    {
        Task TriggerAsync();
    }
}
