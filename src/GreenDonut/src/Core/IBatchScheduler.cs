using System;
using System.Threading.Tasks;

namespace GreenDonut
{
    public interface IBatchScheduler
    {
        void Schedule(Func<ValueTask> dispatch);
    }
}
