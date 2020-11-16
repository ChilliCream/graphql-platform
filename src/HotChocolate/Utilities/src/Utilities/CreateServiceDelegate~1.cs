using System;

namespace HotChocolate.Utilities
{
    public delegate T CreateServiceDelegate<out T>(IServiceProvider services);
}
