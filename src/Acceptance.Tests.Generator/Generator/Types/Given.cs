namespace Generator
{
    internal class Given
    {
        public string InlineQuery()
        {
            return Query.Replace("\n", " ");
        }

        public string Query { get; set; }
        public string Schema { get; set; }
        public string SchemaFile { get; set; }
        public object TestData { get; set; }
        public string TestDataFile { get; set; }
    }
}
