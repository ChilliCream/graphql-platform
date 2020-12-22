using System;
using System.Text;

namespace StrawberryShake.Remove
{
    public class GetFooQueryDocument : IDocument
    {
        private const string _bodyString = "query GetFoo { foo { bar baz { quox } }";
        private static readonly byte[] _body = Encoding.UTF8.GetBytes(_bodyString);

        private GetFooQueryDocument() { }

        public static GetFooQueryDocument Instance { get; } = new();

        public OperationKind Kind => OperationKind.Query;
        public ReadOnlySpan<byte> Body => _body;

        public override string ToString() => _bodyString;
    }
}
