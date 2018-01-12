using System.Threading.Tasks;

namespace Zeus
{
    public interface IRequestProcessor
    {
        Task ExecuteAsync(ISchema schema, IRequest request);
    }


}
