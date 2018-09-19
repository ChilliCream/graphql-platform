using System;
using System.Threading.Tasks;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Execution
{
    public class QueryDirectiveTests
    {
        [Fact]
        public void SimpleSelectionDirectiveWithoutArguments()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result = schema.Execute("{ sayHello @Foo }");

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        public static ISchema CreateSchema()
        {
            return Schema.Create(c =>
            {
                c.RegisterDirective<AppendDotDirective>();
                c.RegisterQueryType<Query>();
            });
        }

        public class Query
        {
            public string SayHello() => "Hello";
        }


        public class AppendDotDirective
            : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
            {
                descriptor.Name("Foo");
                descriptor.Location(DirectiveLocation.Field);
                descriptor.Resolver(async (dctx, rctx, ct) =>
                {
                    string resolverResult =
                        await dctx.ResolveFieldAsync<string>();
                    return resolverResult + ".";
                });
            }
        }
    }
}
