using System.IO;

namespace StrawberryShake.VisualStudio.Utilities
{
    internal sealed class TeeStream : Stream
    {
        private readonly Stream _original;
        private readonly Stream _output;
        private bool _disposed;

        public TeeStream(Stream original, string fileName)
        {
            _original = original;
            _output = new FileStream(fileName, FileMode.Create, FileAccess.Write);
        }

        public TeeStream(Stream original, Stream output)
        {
            _original = original;
            _output = output;
        }

        public override bool CanRead => _original.CanRead;

        public override bool CanSeek => _original.CanSeek;

        public override bool CanWrite => _original.CanWrite;

        public override long Length => _original.Length;

        public override long Position
        {
            get => _original.Position;
            set => _original.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var result = _original.Read(buffer, offset, count);
            WriteToOutput(buffer, offset, count);
            return result;
        }

        public override long Seek(long offset, SeekOrigin origin) =>
            _original.Seek(offset, origin);

        public override void SetLength(long value) =>
            _original.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
        {
            _original.Write(buffer, offset, count);
            WriteToOutput(buffer, offset, count);
        }

        public override void Flush()
        {
            _original.Flush();
        }

        private void WriteToOutput(byte[] buffer, int offset, int count)
        {
            _output.Write(buffer, offset, count);
            _output.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _original.Dispose();
                    _output.Dispose();
                    _disposed = true;
                }
            }

            base.Dispose(disposing);
        }
    }
}
