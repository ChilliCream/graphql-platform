using System.Collections.Generic;
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
            Query = query.Replace("\n", " ");
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

        public Block CreateBlock(Statement header)
        {
            return new Block
            {
                header,
                new Statement($"string query = @\"{Query}\";")
            };
        }
    }
}
