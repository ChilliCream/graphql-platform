using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterExceptionsTests
    {
        [Fact]
        public async Task Exception_Queue_Is_Empty()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_String_Contains()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {string_contains: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_String_Not_Contains()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {string_not_contains: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_String_StartsWith()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {string_starts_with: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_String_Not_StartsWith()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {string_not_starts_with: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_String_EndsWith()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {string_ends_with: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_String_Not_EndsWith()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {string_not_ends_with: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_String_In()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {string_in: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_String_Not_In()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {string_not_in: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_In()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {comparable_in: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_Not_In()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {comparable_not_in: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_Eq()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {comparable: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_Not_Eq()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {comparable_not: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_Gt()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {comparable_gt: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_Not_Gt()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {comparable_not_gt: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_Gte()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {comparable_gte: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_Not_Gte()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {comparable_not_gte: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_Lt()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {comparable_lt: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_Not_Lt()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {comparable_not_lt: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_Lte()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {comparable_lte: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_Not_Lte()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {comparable_not_lte: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_NoErrorMessage_On_Object()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {nested: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Array_Any()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {array_any: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_NoErrorMessage_On_Array_Some()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {array_some: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_NoErrorMessage_On_Array_All()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {array_all: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_NoErrorMessage_On_Array_None()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {array_none: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_ErrorMessage_String_Equals_NoError()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {string: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_NoErrorMessage_String_NotEquals_NoError()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {string_not: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_NoErrorMessage_String_NotEquals()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {string_not: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_NoErrorMessage_On_ComparableNullable_Eq()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {nullableComparable: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Exception_NoErrorMessage_On_ComparableNullable_Not_Eq()
        {
            // arrange
            ISchema schema = CreateSchema();
            IRequestExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {nullableComparable_not: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.ToJson().MatchSnapshot();
        }


        private ISchema CreateSchema()
            => SchemaBuilder.New().AddQueryType<Query>().Create();

        public class Query
        {
            [UseFiltering]
            public IEnumerable<Foo> GetFoos() => new Foo[1];

        }

        public class Foo
        {
            public string String { get; set; } = "Test";

            public int Comparable { get; set; }

            public int? NullableComparable { get; set; }

            public FooNested Nested { get; set; }

            public string[] Array { get; set; }

        }

        public class FooNested
        {
            public string Baz { get; set; }
        }

    }
}
