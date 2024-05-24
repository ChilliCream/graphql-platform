using System;
using System.Collections.Generic;
using HotChocolate.Execution;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class OffsetTypeIntegrationTests
    {
        public static class Schema
        {
            public class Query
            {
                public Offset Hours => Offset.FromHours(2);
                public Offset HoursAndMinutes => Offset.FromHoursAndMinutes(2, 35);
                public Offset ZOffset => Offset.Zero;
            }

            public class Mutation
            {
                public Offset Test(Offset arg)
                    => arg + Offset.FromHoursAndMinutes(1, 5);
            }
        }

        private readonly IRequestExecutor _testExecutor = SchemaBuilder.New()
            .AddQueryType<Schema.Query>()
            .AddMutationType<Schema.Mutation>()
            .AddNodaTime()
            .Create()
            .MakeExecutable();

        [Fact]
        public void QueryReturns()
        {
            var result = _testExecutor.Execute("query { test: hours }");
            Assert.Equal("+02", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void QueryReturnsWithMinutes()
        {
            var result = _testExecutor.Execute("query { test: hoursAndMinutes }");
            Assert.Equal("+02:35", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void QueryReturnsWithZ()
        {
            var result = _testExecutor.Execute("query { test: zOffset }");
            Assert.Equal("Z", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation($arg: Offset!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { {"arg", "+02" }, })
                    .Build());
            Assert.Equal("+03:05", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariableWithMinutes()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation($arg: Offset!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { {"arg", "+02:35" }, })
                    .Build());
            Assert.Equal("+03:40", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation($arg: Offset!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { {"arg", "18:30:13+02" }, })
                    .Build());
            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
        }

        [Fact]
        public void ParsesLiteral()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation { test(arg: \"+02\") }")
                    .Build());
            Assert.Equal("+03:05", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithMinutes()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation { test(arg: \"+02:35\") }")
                    .Build());
            Assert.Equal("+03:40", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithZ()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation { test(arg: \"Z\") }")
                    .Build());
            Assert.Equal("+01:05", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation { test(arg: \"18:30:13+02\") }")
                    .Build());
            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
            Assert.Null(result.ExpectQueryResult().Errors![0].Code);
            Assert.Equal(
                "Unable to deserialize string to Offset",
                result.ExpectQueryResult().Errors![0].Message);
        }

        [Fact]
        public void PatternEmptyThrowSchemaException()
        {
            static object Call() => new OffsetType(Array.Empty<IPattern<Offset>>());
            Assert.Throws<SchemaException>(Call);
        }
    }
}
