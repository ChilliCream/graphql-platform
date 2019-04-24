namespace HotChocolate.Language
{
    public static class AstTestHelper
    {
        public static Location CreateDummyLocation()
        {
            var start = new SyntaxTokenInfo(
                TokenKind.StartOfFile, 0, 0, 1, 1);
            return new Location(start, start);
        }
    }
}
