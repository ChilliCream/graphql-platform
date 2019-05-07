using System;
using System.Threading.Tasks;

namespace HotChocolate.Resolvers
{
    public class GeneratorTestDummy
    {
        public Task<string> GetFooAsync()
        {
            throw new NotSupportedException();
        }

        public Task<string> GetFooAsync(string a)
        {
            throw new NotSupportedException();
        }

        public Task<string> GetFooAsync(string a, int b)
        {
            throw new NotSupportedException();
        }

        public string GetFoo()
        {
            throw new NotSupportedException();
        }

        public string GetFoo(string a)
        {
            throw new NotSupportedException();
        }

        public string GetFoo(string a, int b)
        {
            throw new NotSupportedException();
        }

        public string Bar => throw new NotSupportedException();
    }
}
