using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Lodash
{
    [XmlRoot("test")]
    public class TestCase
    {
        [XmlElement("data")]
        public TestData? Data { get; set; }

        [XmlElement("query")]
        public string? Query { get; set; }

        [XmlElement("result")]
        public TestResult? Result { get; set; }

        public static bool TryParse(
            string fileName,
            [NotNullWhen(true)] out TestCase? testCase)
        {
            SnapshotFullName name = new XunitSnapshotFullNameReader().ReadSnapshotFullName();
            var filePath = System.IO.Path.Combine(
                name.FolderPath,
                "__operations__",
                $"{fileName}.xml");

            RETRY:
            try
            {
                using FileStream fileStream = File.Open(filePath, FileMode.Open);

                XmlSerializer serializer = new(typeof(TestCase));
                testCase = (TestCase?)serializer.Deserialize(fileStream);
                return testCase is not null;

            }
            catch (IOException exception)
            {
                if (exception.Message.Contains("because it is being used by another process"))
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(50));
                    goto RETRY;
                }

                throw;
            }
        }
    }

    public class TestData
    {
        [XmlElement("json")]
        public string Json { get; set; } = null!;
    }

    public class TestResult
    {
        [XmlElement("json")]
        public string? Json { get; set; }

        [XmlElement("error-code")]
        public string? ErrorCode { get; set; }
    }

    public class LodashTestBase
    {
        private static ISchema? _schema;

        protected async ValueTask RunTestByDefinition(string testName)
        {
            if (!TestCase.TryParse(testName, out TestCase? testCase))
            {
                throw new InvalidOperationException("This test configuration is invalid");
            }

            if (testCase.Data is null)
            {
                throw new InvalidOperationException("This test is missing <data>");
            }

            if (testCase.Query is null)
            {
                throw new InvalidOperationException("This test is missing <query>");
            }

            if (testCase.Result is null)
            {
                throw new InvalidOperationException("This test is missing <result>");
            }

            // arrange
            ISchema? schema = await GetSchema();
            DocumentNode parsed = Utf8GraphQLParser.Parse(testCase.Query);
            JsonNode data = JsonNode.Parse(testCase.Data.Json) ??
                throw new InvalidOperationException(
                    "This test has invalid json in <data><json/></data>");
            if (testCase.Result.Json is { } json)
            {
                JsonNode result = JsonNode.Parse(json) ??
                    throw new InvalidOperationException(
                        "This test has invalid json in <result><json/></result>");

                // act
                AggregationJsonRewriter lodashRewriter = parsed.CreateRewriter(schema);
                JsonNode? rewritten = lodashRewriter.Rewrite(data);

                // assert
                Assert.NotNull(rewritten);
                JsonSerializerOptions options = new() { WriteIndented = true };
                Assert.Equal<object?>(
                    result.ToJsonString(options),
                    rewritten?.ToJsonString(options));
            }
            else if (testCase.Result.ErrorCode is { } error)
            {
                // act
                AggregationJsonRewriter lodashRewriter = parsed.CreateRewriter(schema);
                JsonAggregationException? ex = Assert.Throws<JsonAggregationException>(() =>
                {
                    lodashRewriter.Rewrite(data);
                });

                // assert
                Assert.NotNull(ex);
                Assert.Equal<object>(error, ex.Code);
            }
            else
            {
                throw new InvalidOperationException(
                    "Configure either <result><json/></result> or <result><error-code/></result>");
            }
        }

        private async ValueTask<ISchema> GetSchema()
        {
            if (_schema is null)
            {
                IRequestExecutor? executor = await CreateExecutor();
                _schema = executor.Schema;
            }

            return _schema;
        }

        protected ValueTask<IRequestExecutor> CreateExecutor()
        {
            return new ServiceCollection()
                .AddScoped(sp => new Query())
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddAggregationDirectives()
                .BuildRequestExecutorAsync();
        }

        public class Query
        {
            public string? String { get; set; }

            public int? Int { get; set; }

            public double? Float { get; set; }

            public DateTime? DateTime { get; set; }

            public ExampleEnum? Enum { get; set; }

            public string?[]? StringList { get; set; }

            public int?[]? IntList { get; set; }

            public double?[]? FloatList { get; set; }

            public DateTime?[]? DateTimeList { get; set; }

            public ExampleEnum?[]? EnumList { get; set; }

            public string?[]?[]? StringNestedList { get; set; }

            public int?[]?[]? IntNestedList { get; set; }

            public double?[]?[]? FloatNestedList { get; set; }

            public DateTime?[]?[]? DateTimeNestedList { get; set; }

            public ExampleEnum?[]?[]? EnumNestedList { get; set; }

            public Query? Single { get; set; }

            public Query?[]? List { get; set; }

            public Query?[]?[]? NestedList { get; set; }
        }

        public enum ExampleEnum
        {
            First,
            Second,
            Third
        }
    }
}
