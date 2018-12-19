using System;
using System.Threading.Tasks;

namespace HotChocolate.Resolvers
{
    public class GeneratorTestDummyResolver
    {
        public Task<string> GetFooAsync()
        {
            throw new NotImplementedException();
        }

        public Task<string> GetFooAsync(GeneratorTestDummy a)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetFooAsync(GeneratorTestDummy a, string b)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetFooAsync(GeneratorTestDummy a, string b, int c)
        {
            throw new NotImplementedException();
        }

        public string GetFoo()
        {
            throw new NotImplementedException();
        }

        public string GetFoo(GeneratorTestDummy a)
        {
            throw new NotImplementedException();
        }

        public string GetFoo(GeneratorTestDummy a, string b)
        {
            throw new NotImplementedException();
        }

        public string GetFoo(GeneratorTestDummy a, string b, int c)
        {
            throw new NotImplementedException();
        }

        public string Bar => throw new NotImplementedException();
    }
}
