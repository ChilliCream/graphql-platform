namespace Foo
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetHeroQueryDocument
        : global::StrawberryShake.IDocument
    {
        private const global::System.String _bodyString = 
            @"query GetHero {
  hero(episode: NEW_HOPE) {
    __typename
    name
    appearsIn
    ... on Droid {
      id
    }
    ... on Human {
      id
    }
  }
}";
        private static readonly byte[] _body = global::System.Text.Encoding.UTF8.GetBytes(_bodyString);

        private GetHeroQueryDocument()
        {
        }

        public static GetHeroQueryDocument Instance { get; } = new GetHeroQueryDocument();

        public global::StrawberryShake.OperationKind Kind => global::StrawberryShake.OperationKind.Query;

        public global::System.ReadOnlySpan<byte> Body => _body;

        public override string ToString()
        {
            return _bodyString;
        }
    }
}
