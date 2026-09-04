namespace LogAnomalyEngine.Tests.TestInfrastructure;

internal sealed class ChunkedReadStream : Stream
{
    private readonly MemoryStream _innerStream;
    private readonly int _maxBytesPerRead;

    public ChunkedReadStream(byte[] data, int maxBytesPerRead)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytesPerRead);

        _innerStream = new MemoryStream(data, writable: false);
        _maxBytesPerRead = maxBytesPerRead;
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => _innerStream.Length;

    public override long Position
    {
        get => _innerStream.Position;
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return _innerStream.Read(
            buffer,
            offset,
            Math.Min(count, _maxBytesPerRead));
    }

    public override int Read(Span<byte> buffer)
    {
        return _innerStream.Read(
            buffer[..Math.Min(buffer.Length, _maxBytesPerRead)]);
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _innerStream.Dispose();
        }

        base.Dispose(disposing);
    }
}
