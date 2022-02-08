using System.Collections.Concurrent;
using System.IO;

namespace StrawberryShake.CodeGeneration.CSharp;

public class TestStream : Stream
{
    private readonly ConcurrentQueue<byte[]> _queue = new();

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => true;
    public override long Length => 0;
    public override long Position { get; set; }

    public override void Write(byte[] buffer, int offset, int count)
    {
        var chunk = new byte[buffer.Length];
        buffer.CopyTo(chunk, 0);
        _queue.Enqueue(chunk);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_queue.TryDequeue(out var chunk))
        {
            chunk.CopyTo(buffer, 0);
            return chunk.Length;
        }

        return 0;
    }

    public override long Seek(long offset, SeekOrigin origin) => 0;

    public override void SetLength(long value) {  }

    public override void Flush()
    {
    }
}
