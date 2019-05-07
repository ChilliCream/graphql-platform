using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Resolvers.CodeGeneration;
using HotChocolate.Resolvers.Expressions.Parameters;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Resolvers.Expressions
{
    public class ResolverCompilerTests
    {
        [Fact]
        public async Task ResolverPropertyGenerator_Generate()
        {
            // arrange
            Type sourceType = typeof(Resolvers);

            var compiler = new ResolverCompiler(
                Array.Empty<IResolverParameterCompiler>());

            Expression instance = Expression.Constant(new Resolvers());

            // act
            Func<IResolverContext, Task<object>> resolver =
                compiler.CreateResolver(
                    instance,
                    sourceType.GetMember("ObjectTaskResolver").First(),
                    typeof(Resolvers));

            // assert
            Assert.Equal("ObjectResolverResult", (string)await resolver(null));
        }

        public class Resolvers
        {
            public Task<object> ObjectTaskResolver() =>
                Task.FromResult<object>("ObjectResolverResult");

            public Task<string> StringTaskResolver() =>
                Task.FromResult<string>("StringTaskResolver");
        }
    }
}
