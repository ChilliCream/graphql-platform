using System;
using System.Threading.Tasks;

namespace HotChocolate.Resolvers
{
    public class GeneratorTestDummy
    {
        public Task<string> GetFooAsync()
        {
            throw new NotImplementedException();
        }

        public Task<string> GetFooAsync(string a)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetFooAsync(string a, int b)
        {
            throw new NotImplementedException();
        }

        public string GetFoo()
        {
            throw new NotImplementedException();
        }

        public string GetFoo(string a)
        {
            throw new NotImplementedException();
        }

        public string GetFoo(string a, int b)
        {
            throw new NotImplementedException();
        }

        public string Bar => throw new NotImplementedException();
    }
}
