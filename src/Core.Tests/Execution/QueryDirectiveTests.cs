using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Runtime;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Execution
{
    public class QueryDirectiveTests
    {
        [Fact]
        public void OnInvoke_Delegate_ExecuteResolverAndAppendArgument()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result = schema.Execute(
                "{ sayHello @AppendOnInvoke(s: \" abc\") }");

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }


        [Fact]
        public void OnInvoke_SyncGenerated_Result()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result = schema.Execute(
                "{ sayHello @AppendOnInvokeGenSyncWithResult(s: \" def\") }");

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void OnInvoke_SyncGenerated_WithoutResult()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result = schema.Execute(
                "{ sayHello @AppendOnInvokeGenSync(s: \" ghi\") }");

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void OnInvoke_AsyncGenerated_Resolver()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result = schema.Execute(
                "{ sayHello @AppendOnInvokeGenAsyncWithResolver(s: \" jkl\") }");

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        public static ISchema CreateSchema()
        {
            return Schema.Create(c =>
            {
                c.RegisterCustomContext<Dictionary<string, string>>(
                    ExecutionScope.Request,
                    s => new Dictionary<string, string>());

                c.RegisterDirective<AppendOnInvokeDirective>();
                c.RegisterDirective<AppendOnInvokeGeneratedSyncWithResultDirective>();
                c.RegisterDirective<AppendOnInvokeGeneratedSyncDirective>();
                c.RegisterDirective<AppendOnInvokeGeneratedAsyncWithResolver>();

                c.RegisterQueryType<Query>();
            });
        }

        public class Query
        {
            public string SayHello() => "Hello";
        }

        public class AppendOnInvokeDirective
            : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
            {
                descriptor.Name("AppendOnInvoke");
                descriptor.Location(DirectiveLocation.Field);
                descriptor.Argument("s").Type<NonNullType<StringType>>();
                descriptor.OnInvokeResolver(async (ctx, dir, exec, ct) =>
                {
                    string resolverResult = await exec() as string;
                    return resolverResult + " " + dir.GetArgument<string>("s");
                });
            }
        }

        public class AppendOnInvokeGeneratedSyncWithResultDirective
            : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
            {
                descriptor.Name("AppendOnInvokeGenSyncWithResult");
                descriptor.Location(DirectiveLocation.Field);
                descriptor.Argument("s").Type<NonNullType<StringType>>();
                descriptor.OnInvokeResolver<AppendDirectiveMiddleware>(
                    t => t.OnInvokeResolverWithResult(default, default));
            }
        }

        public class AppendOnInvokeGeneratedSyncDirective
            : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
            {
                descriptor.Name("AppendOnInvokeGenSync");
                descriptor.Location(DirectiveLocation.Field);
                descriptor.Argument("s").Type<NonNullType<StringType>>();
                descriptor.OnInvokeResolver<AppendDirectiveMiddleware>(
                    t => t.OnInvokeResolver(default));
            }
        }

        public class AppendOnInvokeGeneratedAsyncWithResolver
            : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
            {
                descriptor.Name("AppendOnInvokeGenAsyncWithResolver");
                descriptor.Location(DirectiveLocation.Field);
                descriptor.Argument("s").Type<NonNullType<StringType>>();
                descriptor.OnInvokeResolver<AppendDirectiveMiddleware>(
                    t => t.OnInvokeResolverAsync(default, default));
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
            public void OnBeforeInvokeResolver(
                [Result]string resolverResult,
                [DirectiveArgument]string s)
            {

            }

            public string OnInvokeResolverWithResult(
                [Result] string result,
                [DirectiveArgument]string s)
            {
                return result + s;
            }

            public string OnInvokeResolver(
                [DirectiveArgument]string s)
            {
                return s;
            }

            public async Task<string> OnInvokeResolverAsync(
                [Resolver]Func<Task<string>> resolver,
                [DirectiveArgument]string s)
            {
                return (await resolver()) + s;
            }



            public string OnAfterInvokeResolverAsync(
                [Result]string resolverResult, [DirectiveArgument]string s)
            {
                return resolverResult + s;
            }
        }
    }
}
