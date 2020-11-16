using System;
using Snapshooter.Xunit;
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
            OrderedDictionary result = builder.Build();

            // assert
            result.MatchSnapshot();
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
            Path rootPath = Path.New("root").Append("field");
            var resolverStatisticsA = new ApolloTracingResolverRecord
            {
                Path = rootPath.Append(0).Append("value").ToList(),
                ParentType = "ParentTypeA",
                FieldName = "FieldNameA",
                ReturnType = "ReturnTypeA",
                StartTimestamp = 1113752444890200,
                EndTimestamp = 1113752454811100
            };
            var resolverStatisticsB = new ApolloTracingResolverRecord
            {
                Path = rootPath.Append(1).Append("value").ToList(),
                ParentType = "ParentTypeB",
                FieldName = "FieldNameB",
                ReturnType = "ReturnTypeB",
                StartTimestamp = 1113752464890200,
                EndTimestamp = 1113752484850000
            };
            var duration = TimeSpan.FromMilliseconds(122);

            builder.SetRequestStartTime(startTime, startTimestamp);
            builder.SetParsingResult(1113752394890300, 1113752402820700);
            builder.SetValidationResult(1113752404890400, 1113752434898800);
            builder.AddResolverResult(resolverStatisticsA);
            builder.AddResolverResult(resolverStatisticsB);
            builder.SetRequestDuration(duration);

            // act
            OrderedDictionary result = builder.Build();

            // assert
            result.MatchSnapshot();
        }
    }
}
