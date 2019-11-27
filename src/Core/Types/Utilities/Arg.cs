using System;

namespace HotChocolate.Types
{
    public static class Arg
    {
        public static T Is<T>()
        {
            throw new NotSupportedException();
        }
    }
}
