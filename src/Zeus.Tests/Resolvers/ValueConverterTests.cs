using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQLParser;
using Newtonsoft.Json;
using Xunit;
using Zeus.Execution;
using Zeus.Resolvers;
using Moq;
using System;
using Zeus.Abstractions;
using System.Linq;

namespace Zeus.Resolvers
{
    public class ValueConverterTests
    {
        [Fact]
        public void Foo()
        {
            Type a = typeof(IEnumerable<string>);
            var y = a.GetGenericArguments();
            var x = a.GetGenericTypeDefinition();
            bool zzz = typeof(IEnumerable<string>).IsAssignableFrom(typeof(List<string>));

        }


    }
}