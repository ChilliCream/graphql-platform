namespace HotChocolate.Language.Utilities
{
    internal static class SyntaxPrinter
    {
        private static readonly SyntaxSerializer _serializer =
            new SyntaxSerializer(new SyntaxSerializerOptions { Indented = true });
        public static string Print(this ISyntaxNode node)
        {
            StringSyntaxWriter writer = StringSyntaxWriter.Rent();

            try
            {
                _serializer.Serialize(node, writer);
                return writer.ToString();
            }
            finally
            {
                StringSyntaxWriter.Return(writer);
            }
        }
    }
}
