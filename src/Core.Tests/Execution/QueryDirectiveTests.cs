using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
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
            IExecutionResult result = schema.Execute("{ sayHello @Dot }");

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void SimpleSelectionDirectiveWithArguments()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result = schema.Execute(
                "{ sayHello @Append(s: \" sir\") }");

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void SimpleSelectionDirectiveWithGeneratedMiddleware()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result = schema.Execute(
                "{ sayHello @Append(s: \" sir\") }");

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        public static ISchema CreateSchema()
        {
            return Schema.Create(c =>
            {
                c.RegisterDirective<AppendDotDirective>();
                c.RegisterDirective<AppendStringDirective>();
                c.RegisterDirective<AppendStringAfterResolveDirective>();
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
                descriptor.Name("Dot");
                descriptor.Location(DirectiveLocation.Field);
                descriptor.OnInvokeResolver(async (ctx, dir, exec, ct) =>
                {
                    string resolverResult = await exec() as string;
                    return resolverResult + ".";
                });
            }
        }

        public class AppendStringDirective
            : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
            {
                descriptor.Name("Append");
                descriptor.Location(DirectiveLocation.Field);
                descriptor.Argument("s").Type<NonNullType<StringType>>();
                descriptor.OnInvokeResolver(async (ctx, dir, exec, ct) =>
                {
                    string resolverResult = await exec() as string;
                    return resolverResult + " " + dir.GetArgument<string>("s");
                });
            }
        }

        public class AppendStringAfterResolveDirective
            : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
            {
                descriptor.Name("AppendOnAfter");
                descriptor.Location(DirectiveLocation.Field);
                descriptor.Argument("s").Type<NonNullType<StringType>>();
                descriptor.OnAfterInvokeResolver<AppendDirectiveMiddleware>(
                    t => t.OnAfterInvokeResolverAsync(default, default));
            }
        }

        public class AppendDirectiveMiddleware
        {
            public string OnAfterInvokeResolverAsync(
                [Result]string resolverResult, string s)
            {
                return resolverResult + s;
            }
        }
    }
}
