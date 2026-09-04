namespace LogAnomalyEngine.Benchmarks.Infrastructure;

internal sealed class LineBoundaryReadStream : Stream
{
    private readonly MemoryStream _innerStream;
    private readonly int _lineLength;
    private readonly bool _alignReadsToLineBoundaries;

    public LineBoundaryReadStream(
        byte[] data,
        int lineLength,
        bool alignReadsToLineBoundaries)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lineLength);

        _innerStream = new MemoryStream(data, writable: false);
        _lineLength = lineLength;
        _alignReadsToLineBoundaries = alignReadsToLineBoundaries;
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

    public void Reset()
    {
        _innerStream.Position = 0;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var readCount = GetReadCount(count);

        return _innerStream.Read(
            buffer,
            offset,
            readCount);
    }

    public override int Read(Span<byte> buffer)
    {
        var readCount = GetReadCount(buffer.Length);

        return _innerStream.Read(buffer[..readCount]);
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

    private int GetReadCount(int requestedCount)
    {
        var positionInLine = (int)(_innerStream.Position % _lineLength);
        var bytesUntilLineBoundary = _lineLength - positionInLine;

        if (_alignReadsToLineBoundaries)
        {
            return Math.Min(
                requestedCount,
                bytesUntilLineBoundary);
        }

        return requestedCount;
    }
}
