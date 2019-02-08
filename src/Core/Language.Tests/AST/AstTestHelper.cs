namespace HotChocolate.Language
{
    public static class AstTestHelper
    {
        public static Location CreateDummyLocation()
        {
            var source = new Source("foo");
            var start = new SyntaxToken(
                TokenKind.StartOfFile, 0, 0, 1, 1, null);
            return new Location(source, start, start);
        }
    }
}
