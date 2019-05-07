using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Resolvers.CodeGeneration;
using HotChocolate.Resolvers.Expressions.Parameters;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Resolvers.Expressions
{
    public class ResolverCompilerTests
    {
        [Fact]
        public async Task Compile_TaskObjMethod_NoParams_Resolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember = type.GetMethod("ObjectTaskResolver");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            string result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("ObjectResolverResult", result);
        }

        [Fact]
        public async Task Compile_TaskStringMethod_NoParams_Resolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember = type.GetMethod("StringTaskResolver");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            string result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("StringTaskResolver", result);
        }

        [Fact]
        public async Task Compile_TaskStringMethod_WithParams_Resolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod("StringTaskResolverWithArg");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.Argument<string>("a")).Returns("abc");
            string result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("abc", result);
        }

        public class Resolvers
        {
            public Task<object> ObjectTaskResolver() =>
                Task.FromResult<object>("ObjectResolverResult");

            public Task<string> StringTaskResolver() =>
                Task.FromResult<string>("StringTaskResolver");

            public Task<string> StringTaskResolverWithArg(string a) =>
                Task.FromResult<string>(a);
        }
    }
}
