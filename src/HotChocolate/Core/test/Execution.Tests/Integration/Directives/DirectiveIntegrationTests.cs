using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Integration.Directives
{
    public class DirectiveIntegrationTests
    {
        [Fact]
        public async Task UniqueDirectives_OnFieldLevel_OverwriteOnesOnObjectLevel()
        {
            // arrange
            IRequestExecutor executor = await CreateExecutorAsync(c => c
                .AddDocumentFromString(
                    "type Query @constant(value: \"foo\") " +
                    "{ bar: String baz: String @constant(value: \"bar\")  }")
                .AddDirectiveType<ConstantDirectiveType>());

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ bar baz }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task UniqueDirectives_FieldSelection_OverwriteTypeSystemOnes()
        {
            // arrange
            IRequestExecutor executor = await CreateExecutorAsync(c => c
                .AddDocumentFromString(
                    "type Query @constant(value: \"foo\") " +
                    "{ bar: String baz: String @constant(value: \"bar\")  }")
                .AddDirectiveType<ConstantDirectiveType>());

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ bar baz @constant(value: \"baz\") }");

            // assert
            result.ToJson().MatchSnapshot();
        }
    }
}
