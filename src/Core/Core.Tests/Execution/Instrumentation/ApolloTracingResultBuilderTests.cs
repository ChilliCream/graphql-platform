using System;
using ChilliCream.Testing;
using Xunit;

namespace HotChocolate.Execution.Instrumentation
{
    public class ApolloTracingResultBuilderTests
    {
        [Fact]
        public void BuildEmptyTracingResult()
        {
            // arrange
            var builder = new ApolloTracingResultBuilder();

            // act
            ApolloTracingResult result = builder.Build();

            // assert
            result.Snapshot();
        }

        [Fact]
        public void BuildTracingResult()
        {
            // arrange
            var builder = new ApolloTracingResultBuilder();
            DateTimeOffset startTime = new DateTime(
                636824022698524527,
                DateTimeKind.Utc);
            const long startTimestamp = 1113752384890500;
            var rootPath = Path.New("root");
            var rosolverStatisticsA = new ApolloTracingResolverStatistics
            {
                Path = rootPath.Append("field").Append(0).Append("value")
                    .ToFieldPathArray(),
                ParentType = "ParentTypeA",
                FieldName = "FieldNameA",
                ReturnType = "ReturnTypeA",
                StartTimestamp = 1113752444890200,
                EndTimestamp = 1113752454811100
            };
            var rosolverStatisticsB = new ApolloTracingResolverStatistics
            {
                Path = rootPath.Append("field").Append(1).Append("value")
                    .ToFieldPathArray(),
                ParentType = "ParentTypeB",
                FieldName = "FieldNameB",
                ReturnType = "ReturnTypeB",
                StartTimestamp = 1113752464890200,
                EndTimestamp = 1113752484850000
            };
            TimeSpan duration = TimeSpan.FromMilliseconds(122);

            builder.SetRequestStartTime(startTime, startTimestamp);
            builder.SetParsingResult(1113752394890300, 1113752402820700);
            builder.SetValidationResult(1113752404890400, 1113752434898800);
            builder.AddResolverResult(rosolverStatisticsA);
            builder.AddResolverResult(rosolverStatisticsB);
            builder.SetRequestDuration(duration);

            // act
            ApolloTracingResult result = builder.Build();

            // assert
            result.Snapshot();
        }
    }
}
