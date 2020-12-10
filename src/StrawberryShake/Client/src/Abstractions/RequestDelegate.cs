using System.Threading.Tasks;

namespace StrawberryShake
{
    public delegate ValueTask RequestDelegate(IOperationRequestContext context);
}
