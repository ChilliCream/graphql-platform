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
        public void OnBeforeInvoke_Delegate_SetState()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result = schema.Execute(
                "{ sayHello @AppendOnBeforeInvoke(s: \"mno\") }");

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void OnBeforeInvoke_SyncGenerated_SetState()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result = schema.Execute(
                "{ sayHello @AppendOnBeforeInvokeGeneratedSyncDirective" +
                "(s: \"pqr\") }");

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void OnBeforeInvoke_AsyncGenerated_SetState()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result = schema.Execute(
                "{ sayHello @AppendOnBeforeInvokeGeneratedAsyncDirective" +
                "(s: \"stu\") }");

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void OnInvoke_Delegate_ExecuteResolverAndAppendArgument()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result = schema.Execute(
                "{ sayHello @AppendOnInvoke(s: \"abc\") }");

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
                "{ sayHello @AppendOnInvokeGenSyncWithResult(s: \"def\") }");

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
                "{ sayHello @AppendOnInvokeGenSync(s: \"ghi\") }");

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
                "{ sayHello @AppendOnInvokeGenAsyncWithResolver(s: \"jkl\") }");

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void OnAfterInvoke_Delegate_AppendToResult()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result = schema.Execute(
                "{ sayHello @AppendOnAfterInvoke(s: \"vw\") }");

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void OnAfterInvoke_SyncGenerated_AppendToResult()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result = schema.Execute(
                "{ sayHello @AppendOnAfterInvokeGeneratedSyncDirective" +
                "(s: \"xyz\") }");

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void OnAfterInvoke_AsyncGenerated_AppendToResult()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result = schema.Execute(
                "{ sayHello @AppendOnAfterInvokeGeneratedAsyncDirective" +
                "(s: \"123\") }");

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

                c.RegisterDirective<OnInvokeDirective>();
                c.RegisterDirective<OnInvokeGeneratedSyncWithResultDirective>();
                c.RegisterDirective<OnInvokeGeneratedSyncDirective>();
                c.RegisterDirective<OnInvokeGeneratedAsyncWithResolver>();

                c.RegisterDirective<OnBeforeInvokeDirective>();
                c.RegisterDirective<OnBeforeInvokeGeneratedSyncDirective>();
                c.RegisterDirective<OnBeforeInvokeGeneratedAsyncDirective>();

                c.RegisterDirective<OnAfterInvokeDirective>();
                c.RegisterDirective<OnAfterInvokeGeneratedSyncDirective>();
                c.RegisterDirective<OnAfterInvokeGeneratedAsyncDirective>();

                c.RegisterQueryType<Query>();
            });
        }

        public class Query
        {
            public string SayHello([State]Dictionary<string, string> state)
            {
                if (state.TryGetValue("cached", out string s))
                {
                    return s;
                }
                return "Hello";
            }
        }

        public class OnBeforeInvokeDirective
            : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
            {
                descriptor.Name("AppendOnBeforeInvoke");
                descriptor.Location(DirectiveLocation.Field);
                descriptor.Argument("s").Type<NonNullType<StringType>>();
                descriptor.OnBeforeInvokeResolver((ctx, dir, ct) =>
                {
                    var dict = ctx.CustomContext<Dictionary<string, string>>();
                    dict["cached"] = dir.GetArgument<string>("s");
                    return Task.CompletedTask;
                });
            }
        }

        public class OnBeforeInvokeGeneratedSyncDirective
            : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
            {
                descriptor.Name("AppendOnBeforeInvokeGeneratedSyncDirective");
                descriptor.Location(DirectiveLocation.Field);
                descriptor.Argument("s").Type<NonNullType<StringType>>();
                descriptor.OnBeforeInvokeResolver<AppendDirectiveMiddleware>(
                    t => t.OnBeforeInvokeResolver(default, default));
            }
        }

        public class OnBeforeInvokeGeneratedAsyncDirective
           : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
            {
                descriptor.Name("AppendOnBeforeInvokeGeneratedAsyncDirective");
                descriptor.Location(DirectiveLocation.Field);
                descriptor.Argument("s").Type<NonNullType<StringType>>();
                descriptor.OnBeforeInvokeResolver<AppendDirectiveMiddleware>(
                    t => t.OnBeforeInvokeResolverAsync(default, default));
            }
        }

        public class OnInvokeDirective
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

        public class OnInvokeGeneratedSyncWithResultDirective
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

        public class OnInvokeGeneratedSyncDirective
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

        public class OnInvokeGeneratedAsyncWithResolver
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


        public class OnAfterInvokeDirective
            : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
            {
                descriptor.Name("AppendOnAfterInvoke");
                descriptor.Location(DirectiveLocation.Field);
                descriptor.Argument("s").Type<NonNullType<StringType>>();
                descriptor.OnAfterInvokeResolver((ctx, dir, res, ct) =>
                {
                    string resolverResult = (string)res + " " +
                        dir.GetArgument<string>("s");
                    return Task.FromResult<object>(resolverResult);
                });
            }
        }

        public class OnAfterInvokeGeneratedSyncDirective
            : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
            {
                descriptor.Name("AppendOnAfterInvokeGeneratedSyncDirective");
                descriptor.Location(DirectiveLocation.Field);
                descriptor.Argument("s").Type<NonNullType<StringType>>();
                descriptor.OnAfterInvokeResolver<AppendDirectiveMiddleware>(
                    t => t.OnAfterInvokeResolver(default, default));
            }
        }

        public class OnAfterInvokeGeneratedAsyncDirective
            : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
            {
                descriptor.Name("AppendOnAfterInvokeGeneratedAsyncDirective");
                descriptor.Location(DirectiveLocation.Field);
                descriptor.Argument("s").Type<NonNullType<StringType>>();
                descriptor.OnAfterInvokeResolver<AppendDirectiveMiddleware>(
                    t => t.OnAfterInvokeResolverAsync(default, default));
            }
        }

        public class AppendDirectiveMiddleware
        {
            public void OnBeforeInvokeResolver(
                [State]Dictionary<string, string> state,
                [DirectiveArgument]string s)
            {
                state["cached"] = s;
            }

            public Task OnBeforeInvokeResolverAsync(
                [State]Dictionary<string, string> state,
                [DirectiveArgument]string s)
            {
                state["cached"] = s;
                return Task.CompletedTask;
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

            public string OnAfterInvokeResolver(
                [Result]string resolverResult,
                [DirectiveArgument]string s)
            {
                return resolverResult + s;
            }

            public Task<string> OnAfterInvokeResolverAsync(
                [Result]string resolverResult,
                [DirectiveArgument]string s)
            {
                return Task.FromResult(resolverResult + s);
            }
        }
    }
}
