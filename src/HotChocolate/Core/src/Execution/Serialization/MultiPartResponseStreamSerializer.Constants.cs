namespace HotChocolate.Execution.Serialization
{
    public sealed partial class MultiPartResponseStreamSerializer
    {
        private static byte[] ContentType { get; } =
        {
            (byte)'C', (byte)'o', (byte)'n', (byte)'t', (byte)'e', (byte)'n', (byte)'t',
            (byte)'-', (byte)'T', (byte)'y', (byte)'p', (byte)'e', (byte)':', (byte)' ',
            (byte)'a', (byte)'p', (byte)'p', (byte)'l', (byte)'i', (byte)'c', (byte)'a',
            (byte)'t', (byte)'i', (byte)'o', (byte)'n', (byte)'/', (byte)'j', (byte)'s',
            (byte)'o', (byte)'n', (byte)';', (byte)' ', (byte)'c', (byte)'h', (byte)'a',
            (byte)'r', (byte)'s', (byte)'e', (byte)'t', (byte)'=', (byte)'u', (byte)'t',
            (byte)'f', (byte)'-', (byte)'8'
        };

        private static byte[] ContentLength { get; } =
        {
            (byte)'C', (byte)'o', (byte)'n', (byte)'t', (byte)'e', (byte)'n', (byte)'t',
            (byte)'-', (byte)'L', (byte)'e', (byte)'n', (byte)'g', (byte)'t', (byte)'h',
            (byte)':', (byte)' '
        };

        private static byte[] Start { get; } = { (byte)'-', (byte)'-', (byte)'-'};

        private static byte[] End { get; } = { (byte)'-', (byte)'-', (byte)'-', (byte)'-' };

        private static byte[] CrLf { get; } = { (byte)'\r',(byte)'\n' };
    }
}
