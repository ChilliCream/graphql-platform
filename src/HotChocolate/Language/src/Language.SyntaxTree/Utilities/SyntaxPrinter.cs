namespace HotChocolate.Language.Utilities
{
    internal static class SyntaxPrinter
    {
        private static readonly SyntaxSerializer _serializer =
            new(new SyntaxSerializerOptions { Indented = true });
        private static readonly SyntaxSerializer _serializerNoIndent =
            new(new SyntaxSerializerOptions { Indented = false });

        public static string Print(this ISyntaxNode node, bool indented)
        {
            StringSyntaxWriter writer = StringSyntaxWriter.Rent();

            try
            {
                if (indented)
                {
                    _serializer.Serialize(node, writer);
                }
                else
                {
                    _serializerNoIndent.Serialize(node, writer);
                }
                return writer.ToString();
            }
            finally
            {
                StringSyntaxWriter.Return(writer);
            }
        }
    }
}
