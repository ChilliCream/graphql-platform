using System;

namespace GreenDonut
{
    public interface IBatchScheduler
    {
        void Schedule(Action dispatch);
    }
}
