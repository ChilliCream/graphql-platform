using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class ErrorBehaviourTests
    {
        [Fact]
        public async Task SyntaxError()
        {
            // arrange
            var query = "{ error1";
            var i = 0;

            // act
            IExecutionResult result = await ExecuteQuery(query, () => i++);

            // assert
            Assert.NotNull(result.Errors);
            Assert.Equal(1, i);
            result.MatchSnapshot(options =>
                options.IgnoreField("Errors[0].Exception"));
        }

        [Fact]
        public async Task AsyncMethod_NoAwait_Throw_ApplicationError()
        {
            // arrange
            var query = "{ error1 }";
            var i = 0;

            // act
            IExecutionResult result = await ExecuteQuery(query, () => i++);

            // assert
            Assert.NotNull(result.Errors);
            Assert.Equal(1, i);
            result.MatchSnapshot(options =>
                options.IgnoreField("Errors[0].Exception"));
        }

        [Fact]
        public async Task AsyncMethod_Await_Throw_ApplicationError()
        {
            // arrange
            var i = 0;
            var query = "{ error4 }";

            // act
            IExecutionResult result = await ExecuteQuery(query, () => i++);

            // assert
            Assert.NotNull(result.Errors);
            Assert.Equal(1, i);
            result.MatchSnapshot(options =>
                options.IgnoreField("Errors[0].Exception"));
        }

        [Fact]
        public async Task SyncMethod_Throw_ApplicationError()
        {
            // arrange
            var i = 0;
            var query = "{ error7 }";

            // act
            IExecutionResult result = await ExecuteQuery(query, () => i++);

            // assert
            Assert.NotNull(result.Errors);
            Assert.Equal(1, i);
            result.MatchSnapshot(options =>
                options.IgnoreField("Errors[0].Exception"));
        }

        [Fact]
        public async Task Property_Throw_ApplicationError()
        {
            // arrange
            var query = "{ error10 }";

            // act
            IExecutionResult result = await ExecuteQuery(query, () => { });

            // assert
            Assert.NotNull(result.Errors);
            result.MatchSnapshot(options =>
                options.IgnoreField("Errors[0].Exception"));
        }

        [Fact]
        public async Task AsyncMethod_NoAwait_Throw_UnexpectedError()
        {
            // arrange
            var i = 0;
            var query = "{ error2 }";

            // act
            IExecutionResult result = await ExecuteQuery(query, () => i++);

            // assert
            Assert.NotNull(result.Errors);
            Assert.Equal(1, i);
            result.MatchSnapshot(options =>
                options.IgnoreField("Errors[0].Exception"));
        }

        [Fact]
        public async Task AsyncMethod_Await_Throw_UnexpectedError()
        {
            // arrange
            var i = 0;
            var query = "{ error5 }";

            // act
            IExecutionResult result = await ExecuteQuery(query, () => i++);

            // assert
            Assert.NotNull(result.Errors);
            Assert.Equal(1, i);
            result.MatchSnapshot(options =>
                options.IgnoreField("Errors[0].Exception"));
        }

        [Fact]
        public async Task SyncMethod_Throw_UnexpectedError()
        {
            // arrange
            var i = 0;
            var query = "{ error8 }";

            // act
            IExecutionResult result = await ExecuteQuery(query, () => i++);

            // assert
            Assert.NotNull(result.Errors);
            Assert.Equal(1, i);
            result.MatchSnapshot(options =>
                options.IgnoreField("Errors[0].Exception"));
        }

        [Fact]
        public async Task Property_Throw_UnexpectedError()
        {
            // arrange
            var i = 0;
            var query = "{ error11 }";

            // act
            IExecutionResult result = await ExecuteQuery(query, () => i++);

            // assert
            Assert.NotNull(result.Errors);
            Assert.Equal(1, i);
            result.MatchSnapshot(options =>
                options.IgnoreField("Errors[0].Exception"));
        }

        [Fact]
        public async Task AsyncMethod_NoAwait_Return_ApplicationError()
        {
            // arrange
            var i = 0;
            var query = "{ error3 }";

            // act
            IExecutionResult result = await ExecuteQuery(query, () => i++);

            // assert
            Assert.NotNull(result.Errors);
            Assert.Equal(1, i);
            result.MatchSnapshot(options =>
                options.IgnoreField("Errors[0].Exception"));
        }

        [Fact]
        public async Task AsyncMethod_Await_Return_ApplicationError()
        {
            // arrange
            var i = 0;
            var query = "{ error6 }";

            // act
            IExecutionResult result = await ExecuteQuery(query, () => i++);

            // assert
            Assert.NotNull(result.Errors);
            Assert.Equal(1, i);
            result.MatchSnapshot(options =>
                options.IgnoreField("Errors[0].Exception"));
        }

        [Fact]
        public async Task SyncMethod_Return_ApplicationError()
        {
            // arrange
            var i = 0;
            var query = "{ error9 }";

            // act
            IExecutionResult result = await ExecuteQuery(query, () => i++);

            // assert
            Assert.NotNull(result.Errors);
            Assert.Equal(1, i);
            result.MatchSnapshot(options =>
                options.IgnoreField("Errors[0].Exception"));
        }

        [Fact]
        public async Task Property_Return_ApplicationError()
        {
            // arrange
            var i = 0;
            var query = "{ error12 }";

            // act
            IExecutionResult result = await ExecuteQuery(query, () => i++);

            // assert
            Assert.NotNull(result.Errors);
            Assert.Equal(1, i);
            result.MatchSnapshot(options =>
                options.IgnoreField("Errors[0].Exception"));
        }

        [Fact]
        public async Task Property_Return_UnexpectedErrorWithPath()
        {
            // arrange
            var i = 0;
            var query = "{ error13 { bar } }";

            // act
            IExecutionResult result = await ExecuteQuery(query, () => i++);

            // assert
            Assert.NotNull(result.Errors);
            Assert.Equal(1, i);
            result.MatchSnapshot(options =>
                options.IgnoreField("Errors[0].Exception"));
        }

        [Fact]
        public async Task RootTypeNotDefined()
        {
            // arrange
            var query = "mutation { foo }";

            var schema = Schema.Create(
                "type Query { foo: String }",
                c => c.Use(next => context => default(ValueTask)));
            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(query);

            // assert
            result.MatchSnapshot(options =>
                options.IgnoreField("Errors[0].Exception"));
        }

        [Fact]
        public async Task RootFieldNotDefined()
        {
            // arrange
            var query = "mutation { foo }";

            var schema = Schema.Create(
                "type Query { a: String } type Mutation { bar: String }",
                c => c.Use(next => context => default(ValueTask)));
            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(query);

            // assert
            result.MatchSnapshot(options =>
                options.IgnoreField("Errors[0].Exception"));
        }

        [Fact]
        public async Task ErrorFilterHandlesException()
        {
            // arrange
            var query = "{ error14 }";

            ISchema schema = CreateSchema();
            IQueryExecutor executor = schema.MakeExecutable(b =>
                b.AddErrorFilter(error =>
                {
                    if (error.Exception is ArgumentException ex)
                    {
                        return error.WithMessage(ex.Message);
                    }
                    return error;
                })
                .UseDefaultPipeline(new QueryExecutionOptions
                {
                    IncludeExceptionDetails = false
                }));

            // act
            IExecutionResult result = await executor.ExecuteAsync(query);

            // assert
            result.MatchSnapshot(options =>
                options.IgnoreField("Errors[0].Exception"));
        }

        private async Task<IExecutionResult> ExecuteQuery(
            string query,
            Action errorHandled)
        {
            IQueryExecutor queryExecutor = CreateSchema().MakeExecutable(
                b => b.UseDefaultPipeline().AddErrorFilter(error =>
                {
                    errorHandled();
                    return error;
                }));

            return await queryExecutor.ExecuteAsync(query);
        }

        private ISchema CreateSchema()
        {
            return Schema.Create(c =>
            {
                c.Options.StrictValidation = true;
                c.RegisterQueryType<QueryType>();
            });
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
