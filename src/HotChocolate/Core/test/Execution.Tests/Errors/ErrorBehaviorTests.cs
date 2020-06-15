using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Tests;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class ErrorBehaviorTests
    {
        [Fact]
        public Task SyntaxError()
        {
            Snapshot.FullName();
            return ExpectError("{ error1");
        }

        [Fact]
        public Task AsyncMethod_NoAwait_Throw_ApplicationError()
        {
            Snapshot.FullName();
            return ExpectError("{ error1 }");
        }

        [Fact]
        public Task AsyncMethod_Await_Throw_ApplicationError()
        {
            Snapshot.FullName();
            return ExpectError("{ error4 }");
        }

        [Fact]
        public Task SyncMethod_Throw_ApplicationError()
        {

            Snapshot.FullName();
            return ExpectError("{ error7 }");
        }

        [Fact]
        public Task Property_Throw_ApplicationError()
        {
            Snapshot.FullName();
            return ExpectError("{ error10 }");
        }

        [Fact]
        public Task AsyncMethod_NoAwait_Throw_UnexpectedError()
        {
            Snapshot.FullName();
            return ExpectError("{ error2 }");
        }

        [Fact]
        public Task AsyncMethod_Await_Throw_UnexpectedError()
        {
            Snapshot.FullName();
            return ExpectError("{ error5 }");
        }

        [Fact]
        public Task SyncMethod_Throw_UnexpectedError()
        {
            Snapshot.FullName();
            return ExpectError("{ error8 }");
        }

        [Fact]
        public Task Property_Throw_UnexpectedError()
        {
            Snapshot.FullName();
            return ExpectError("{ error11 }");
        }

        [Fact]
        public Task AsyncMethod_NoAwait_Return_ApplicationError()
        {
            Snapshot.FullName();
            return ExpectError("{ error3 }");
        }

        [Fact]
        public Task AsyncMethod_Await_Return_ApplicationError()
        {
            Snapshot.FullName();
            return ExpectError("{ error6 }");
        }

        [Fact]
        public Task SyncMethod_Return_ApplicationError()
        {
            Snapshot.FullName();
            return ExpectError("{ error9 }");
        }

        [Fact]
        public Task Property_Return_ApplicationError()
        {
            Snapshot.FullName();
            return ExpectError("{ error12 }");
        }

        [Fact]
        public Task Property_Return_UnexpectedErrorWithPath()
        {
            Snapshot.FullName();
            return ExpectError("{ error13 }", 0);
        }

        [Fact]
        public Task RootTypeNotDefined()
        {
            Snapshot.FullName();
            return TestHelper.ExpectError(
                "type Query { foo: String }",
                "mutation { foo }");
        }

        [Fact]
        public Task RootFieldNotDefined()
        {
            Snapshot.FullName();
            return TestHelper.ExpectError(
                "type Query { a: String } type Mutation { bar: String }",
                "mutation { foo }");
        }

        [Fact]
        public Task ErrorFilterHandlesException()
        {
            Snapshot.FullName();
            return TestHelper.ExpectError(
                "{ error14 }",
                b => b
                    .AddQueryType<QueryType>()
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = false));
        }

        private async Task ExpectError(
            string query,
            int expectedErrorCount = 1)
        {
            int errors = 0;

            await TestHelper.ExpectError(
                query,
                b => b
                    .AddQueryType<QueryType>()
                    .AddErrorFilter(error =>
                    {
                        errors++;
                        return error;
                    }));

            Assert.Equal(expectedErrorCount, errors);
        }

        public class QueryType
            : ObjectType<Query>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Query> descriptor)
            {
                descriptor.Field(t => t.Error3()).Type<StringType>();
                descriptor.Field(t => t.Error6()).Type<StringType>();
                descriptor.Field(t => t.Error9()).Type<StringType>();
                descriptor.Field(t => t.Error12).Type<StringType>();
            }
        }

        public class Query
        {
            public Task<string> Error1()
            {
                throw new QueryException("query error 1");
            }

            public Task<string> Error2()
            {
                throw new Exception("query error 2");
            }

            public Task<object> Error3()
            {
                return Task.FromResult<object>(
                    ErrorBuilder.New()
                        .SetMessage("query error 3")
                        .Build());
            }

            public async Task<string> Error4()
            {
                await Task.Delay(1);
                throw new QueryException("query error 4");
            }

            public async Task<string> Error5()
            {
                await Task.Delay(1);
                throw new Exception("query error 5");
            }

            public async Task<object> Error6()
            {
                await Task.Delay(1);
                return await Task.FromResult<object>(
                    ErrorBuilder.New()
                        .SetMessage("query error 6")
                        .Build());
            }

            public string Error7()
            {
                throw new QueryException("query error 7");
            }

            public string Error8()
            {
                throw new Exception("query error 8");
            }

            public object Error9()
            {
                return ErrorBuilder.New()
                    .SetMessage("query error 9")
                    .Build();
            }

            public string Error10 => throw new QueryException("query error 10");

            public string Error11 => throw new Exception("query error 11");

            public object Error12 => ErrorBuilder.New()
                .SetMessage("query error 12")
                .Build();

            public Foo Error13 => new Foo();

            public string Error14 => throw new ArgumentNullException("Error14");
        }

        public class Foo
        {
            public string Bar => throw new Exception("baz");
        }
    }
}
