#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetPeopleQueryDocument
        : global::StrawberryShake.IDocument
    {
        private const global::System.String _bodyString = 
            @"query GetPeople {
  people(order_by: { name: ASC }) {
    __typename
    nodes {
      __typename
      name
      email
      isOnline
      lastSeen
      ... on Person {
        id
      }
    }
  }
}";
        private static readonly byte[] _body = global::System.Text.Encoding.UTF8.GetBytes(_bodyString);

        private GetPeopleQueryDocument()
        {
        }

        public static GetPeopleQueryDocument Instance { get; } = new GetPeopleQueryDocument();

        public global::StrawberryShake.OperationKind Kind => global::StrawberryShake.OperationKind.Query;

        public global::System.ReadOnlySpan<byte> Body => _body;

        public override string ToString()
        {
            return _bodyString;
        }
    }
}
