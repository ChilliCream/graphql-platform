namespace Generator
{
    internal class Background
    {
        public static Background Empty { get; } =
            new Background(string.Empty, string.Empty, null, string.Empty);

        public Background(
            string schema,
            string schemaFile,
            object testData,
            string testDataFile)
        {
            Schema = schema;
            SchemaFile = schemaFile;
            TestData = testData;
            TestDataFile = testDataFile;
        }

        public string Schema { get; }
        public string SchemaFile { get; }
        public object TestData { get; }
        public string TestDataFile { get; }

        public void Create()
        {
        }
    }
}
