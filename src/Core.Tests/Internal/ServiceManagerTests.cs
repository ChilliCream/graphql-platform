using System;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Internal
{
    public class ServiceManagerTests
    {
        [Fact]
        public void Foo()
        {
            Microsoft.Extensions.DependencyInjection.DefaultServiceProviderFactory d = new DefaultServiceProviderFactory();
            IServiceProvider s = d.CreateServiceProvider(new ServiceCollection());
            ServiceManager serviceManager = new ServiceManager(s);
            object o = serviceManager.GetService(typeof(ObjectType<Q123>));
            Assert.NotNull(o);

        }
    }

    public class Q123
    {
        public string Test() => "jjj";
    }
}
