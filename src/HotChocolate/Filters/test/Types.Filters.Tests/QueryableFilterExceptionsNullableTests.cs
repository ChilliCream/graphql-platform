using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterExceptionsNullableTests
    {
        [Fact]
        public async Task Exception_NoErrorMessage_StringNullable_Equals()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {stringNullable: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Null(result.Errors);
        }

        [Fact]
        public async Task Exception_NoErrorMessage_StringNullable_NotEquals()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {stringNullable_not: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Null(result.Errors);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_StringNullable_Contains()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {stringNullable_contains: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_StringNullable_Not_Contains()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {stringNullable_not_contains: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_StringNullable_StartsWith()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {stringNullable_starts_with: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_StringNullable_Not_StartsWith()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {stringNullable_not_starts_with: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_StringNullable_EndsWith()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {stringNullable_ends_with: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_StringNullable_Not_EndsWith()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {stringNullable_not_ends_with: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_StringNullable_In()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {stringNullable_in: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_StringNullable_Not_In()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {stringNullable_not_in: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_String_Equals()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_String_NotEquals()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_String_Contains()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_String_Not_Contains()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_String_StartsWith()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_String_Not_StartsWith()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_String_EndsWith()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_String_Not_EndsWith()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_String_In()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_String_Not_In()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_In()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_Not_In()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_Eq()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_Not_Eq()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_Gt()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_Not_Gt()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_Gte()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_Not_Gte()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_Lt()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_Not_Lt()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_Lte()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_Not_Lte()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_ComparableNullable_In()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {comparableNullable_in: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_ComparableNullable_Not_In()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {comparableNullable_not_in: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_NoErrorMessage_On_ComparableNullable_Eq()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {comparableNullable: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Null(result.Errors);
        }

        [Fact]
        public async Task Exception_NoErrorMessage_On_ComparableNullable_Not_Eq()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {comparableNullable_not: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Null(result.Errors);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_ComparableNullable_Gt()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {comparableNullable_gt: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_ComparableNullable_Not_Gt()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {comparableNullable_not_gt: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_NullableGte()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {comparableNullable_gte: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_Not_NullableGte()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {comparableNullable_not_gte: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_ComparableNullable_Lt()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {comparableNullable_lt: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_ComparableNullable_Not_Lt()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {comparableNullable_not_lt: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_NullableLte()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {comparableNullable_lte: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Comparable_Not_NullableLte()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {comparableNullable_not_lte: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Object()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_NoErrorMessage_On_ObjectNullable()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {nestedNullable: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Null(result.Errors);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Array_Any()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Array_Some()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Array_All()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_Array_None()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
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
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_ErrorMessage_On_ArrayNullable_Any()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {arrayNullable_any: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Single(result.Errors);
            Assert.Empty((result as IReadOnlyQueryResult)?.Data["foos"] as IEnumerable<object>);
        }

        [Fact]
        public async Task Exception_NoErrorMessage_On_ArrayNullable_Some()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {arrayNullable_some: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Null(result.Errors);
        }

        [Fact]
        public async Task Exception_NoErrorMessage_On_ArrayNullable_All()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {arrayNullable_all: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Null(result.Errors);
        }

        [Fact]
        public async Task Exception_NoErrorMessage_On_ArrayNullable_None()
        {
            // arrange
            ISchema schema = CreateSchema();
            IQueryExecutor executer = schema.MakeExecutable();
            const string query = @"
            {
                foos(where: {arrayNullable_none: null})
                {
                    string 
                }
            }";

            // act
            IExecutionResult result = await executer.ExecuteAsync(query).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
            Assert.Null(result.Errors);
        }

        private ISchema CreateSchema()
            => SchemaBuilder.New().AddQueryType<Query>().Create();

        public class Query
        {
            [UseFiltering]
            public IEnumerable<Foo> GetFoos() => new Foo[1];
        }

#nullable enable
        public class Foo
        {
            public string String { get; set; } = "Test";

            public string? StringNullable { get; set; } = "Test";

            public int Comparable { get; set; }

            public int? ComparableNullable { get; set; }

            public FooNested Nested { get; set; } = null!;

            public FooNested? NestedNullable { get; set; }

            public string[] Array { get; set; } = null!;

            public string[]? ArrayNullable { get; set; }
        }
#nullable disable

        public class FooNested
        {
            public string Baz { get; set; }
        }
    }
}
