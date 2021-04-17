using System;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Data.Integration.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            IRequestExecutorBuilder builder = new ServiceCollection().AddGraphQL();
        }
    }
}
