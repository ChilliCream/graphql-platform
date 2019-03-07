using System.Collections.Generic;
using System.Text.RegularExpressions;
using Generator.ClassGenerator;

namespace Generator
{
    internal class Given
    {
        public Given(
            string query,
            string schema,
            string schemaFile,
            object testData,
            string testDataFile)
        {
            var inlineQuery = query.Replace("\n", " ");
            Query = Regex.Replace(inlineQuery, @"\s+", " ");
            Schema = schema;
            SchemaFile = schemaFile;
            TestData = testData;
            TestDataFile = testDataFile;
        }

        public string Query { get; }
        public string Schema { get; }
        public string SchemaFile { get; }
        public object TestData { get; }
        public string TestDataFile { get; }

        public Block CreateBlock()
        {
            return new Block
            {
                new Statement($"string query = @\"{Query}\";")
            };
        }
    }
}
