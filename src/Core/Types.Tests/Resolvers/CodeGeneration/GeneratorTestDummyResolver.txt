using System;
using System.Threading.Tasks;

namespace HotChocolate.Resolvers
{
    public class GeneratorTestDummyResolver
    {
        public Task<string> GetFooAsync()
        {
            throw new NotSupportedException();
        }

        public Task<string> GetFooAsync(GeneratorTestDummy a)
        {
            throw new NotSupportedException();
        }

        public Task<string> GetFooAsync(GeneratorTestDummy a, string b)
        {
            throw new NotSupportedException();
        }

        public Task<string> GetFooAsync(GeneratorTestDummy a, string b, int c)
        {
            throw new NotSupportedException();
        }

        public string GetFoo()
        {
            throw new NotSupportedException();
        }

        public string GetFoo(GeneratorTestDummy a)
        {
            throw new NotSupportedException();
        }

        public string GetFoo(GeneratorTestDummy a, string b)
        {
            throw new NotSupportedException();
        }

        public string GetFoo(GeneratorTestDummy a, string b, int c)
        {
            throw new NotSupportedException();
        }

        public string Bar => throw new NotSupportedException();
    }
}
