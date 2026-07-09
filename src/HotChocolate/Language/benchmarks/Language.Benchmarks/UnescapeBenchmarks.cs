using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HotChocolate.Language;

namespace HotChocolate.Language.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob(RuntimeMoniker.Net10_0)]
public class UnescapeBenchmarks
{
    private byte[] _noEscapes = null!;
    private byte[] _singleEscape = null!;
    private byte[] _multipleEscapes = null!;
    private byte[] _unicodeEscapes = null!;
    private byte[] _blockString = null!;

    [GlobalSetup]
    public void Setup()
    {
        _noEscapes = Encoding.UTF8.GetBytes("This is a simple string with no escape characters at all and it is moderately long");
        _singleEscape = Encoding.UTF8.GetBytes("This is a string with a single\\nnewline escape in it somewhere");
        _multipleEscapes = Encoding.UTF8.GetBytes("Line1\\nLine2\\tTabbed\\rReturn\\\\Backslash\\\"Quote\\/Slash");
        _unicodeEscapes = Encoding.UTF8.GetBytes("Unicode: \\u0041\\u0042\\u0043 and \\u00e9\\u00e8\\u00ea more text");
        _blockString = Encoding.UTF8.GetBytes("This is a block string\n  with indentation\n  and multiple lines\n  but no escapes needed");
    }

    [Benchmark]
    public int UnescapeNoEscapes()
    {
        ReadOnlySpan<byte> input = _noEscapes;
        Span<byte> output = stackalloc byte[_noEscapes.Length];
        Utf8Helper.Unescape(in input, ref output, isBlockString: false);
        return output.Length;
    }

    [Benchmark]
    public int UnescapeSingleEscape()
    {
        ReadOnlySpan<byte> input = _singleEscape;
        Span<byte> output = stackalloc byte[_singleEscape.Length];
        Utf8Helper.Unescape(in input, ref output, isBlockString: false);
        return output.Length;
    }

    [Benchmark]
    public int UnescapeMultipleEscapes()
    {
        ReadOnlySpan<byte> input = _multipleEscapes;
        Span<byte> output = stackalloc byte[_multipleEscapes.Length];
        Utf8Helper.Unescape(in input, ref output, isBlockString: false);
        return output.Length;
    }

    [Benchmark]
    public int UnescapeUnicodeEscapes()
    {
        ReadOnlySpan<byte> input = _unicodeEscapes;
        Span<byte> output = stackalloc byte[_unicodeEscapes.Length];
        Utf8Helper.Unescape(in input, ref output, isBlockString: false);
        return output.Length;
    }

    [Benchmark]
    public int UnescapeBlockString()
    {
        ReadOnlySpan<byte> input = _blockString;
        Span<byte> output = stackalloc byte[_blockString.Length];
        Utf8Helper.Unescape(in input, ref output, isBlockString: true);
        return output.Length;
    }
}
