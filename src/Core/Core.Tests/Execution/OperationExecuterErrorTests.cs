using System;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Execution
{
    public class OperationExecuterErrorTests
    {
        [Fact]
        public async Task AsyncMethod_NoAwait_Throw_ApplicationError()
        {
            // arrange
            string query = "{ error1 }";

            // act
            IExecutionResult result = await ExecuteQuery(query);

            // assert
            Assert.NotNull(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task AsyncMethod_Await_Throw_ApplicationError()
        {
            // arrange
            string query = "{ error4 }";

            // act
            IExecutionResult result = await ExecuteQuery(query);

            // assert
            Assert.NotNull(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task SyncMethod_Throw_ApplicationError()
        {
            // arrange
            string query = "{ error7 }";

            // act
            IExecutionResult result = await ExecuteQuery(query);

            // assert
            Assert.NotNull(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task Property_Throw_ApplicationError()
        {
            // arrange
            string query = "{ error10 }";

            // act
            IExecutionResult result = await ExecuteQuery(query);

            // assert
            Assert.NotNull(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task AsyncMethod_NoAwait_Throw_UnexpectedError()
        {
            // arrange
            string query = "{ error2 }";

            // act
            IExecutionResult result = await ExecuteQuery(query);

            // assert
            Assert.NotNull(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task AsyncMethod_Await_Throw_UnexpectedError()
        {
            // arrange
            string query = "{ error5 }";

            // act
            IExecutionResult result = await ExecuteQuery(query);

            // assert
            Assert.NotNull(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task SyncMethod_Throw_UnexpectedError()
        {
            // arrange
            string query = "{ error8 }";

            // act
            IExecutionResult result = await ExecuteQuery(query);

            // assert
            Assert.NotNull(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task Property_Throw_UnexpectedError()
        {
            // arrange
            string query = "{ error11 }";

            // act
            IExecutionResult result = await ExecuteQuery(query);

            // assert
            Assert.NotNull(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task AsyncMethod_NoAwait_Return_ApplicationError()
        {
            // arrange
            string query = "{ error3 }";

            // act
            IExecutionResult result = await ExecuteQuery(query);

            // assert
            Assert.NotNull(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task AsyncMethod_Await_Return_ApplicationError()
        {
            // arrange
            string query = "{ error6 }";

            // act
            IExecutionResult result = await ExecuteQuery(query);

            // assert
            Assert.NotNull(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task SyncMethod_Return_ApplicationError()
        {
            // arrange
            string query = "{ error9 }";

            // act
            IExecutionResult result = await ExecuteQuery(query);

            // assert
            Assert.NotNull(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task Property_Return_ApplicationError()
        {
            // arrange
            string query = "{ error12 }";

            // act
            IExecutionResult result = await ExecuteQuery(query);

            // assert
            Assert.NotNull(result.Errors);
            result.Snapshot();
        }

         [Fact]
        public async Task Property_Return_UnexpectedErrorWithPath()
        {
            // arrange
            string query = "{ error13 { bar } }";

            // act
            IExecutionResult result = await ExecuteQuery(query);

            // assert
            Assert.NotNull(result.Errors);
            result.Snapshot();
        }

        private async Task<IExecutionResult> ExecuteQuery(string query)
        {
            Schema schema = CreateSchema();
            return await schema.ExecuteAsync(query);
        }

        private Schema CreateSchema()
        {

            return Schema.Create(c =>
            {
                c.Options.ExecutionTimeout = TimeSpan.FromSeconds(30);
                c.Options.StrictValidation = true;
                c.RegisterQueryType<QueryType>();
            });
        }

        public class QueryType
            : ObjectType<Query>
        {
            protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
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
                return Task.FromResult<object>(new QueryError("query error 3"));
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
                return await Task.FromResult<object>(new QueryError("query error 6"));
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
                return new QueryError("query error 9");
            }

            public string Error10 => throw new QueryException("query error 10");

            public string Error11 => throw new Exception("query error 11");

            public object Error12 => new QueryError("query error 12");

            public Foo Error13 => new Foo();
        }

        public class Foo
        {
            public string Bar => throw new Exception("baz");
        }
    }
}
