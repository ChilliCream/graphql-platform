using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Serialization;

[RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
public class JsonResultFormatterBenchmark
{
    private IQueryResult _bigResult = CreateBigResult();
    private IQueryResult _smallResult = CreateSmall();
    private JsonResultFormatter _formatter = new();

    [Benchmark]
    public async Task Small_Original()
    {
        await _formatter.FormatAsync(_smallResult, NullStream.Instance, CancellationToken.None);
    }

    [Benchmark]
    public async Task Small_Pipewriter()
    {
        await _formatter.PipeWriter(_smallResult, NullStream.Instance, CancellationToken.None);
    }

    [Benchmark]
    public async Task Small_Original_Parallel()
    {
        var tasks = new Task[10];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = _formatter
                .FormatAsync(_smallResult, NullStream.Instance, CancellationToken.None)
                .AsTask();
        }

        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task Small_Pipewriter_Parallel()
    {
        var tasks = new Task[10];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = _formatter
                .PipeWriter(_smallResult, NullStream.Instance, CancellationToken.None)
                .AsTask();
        }

        await Task.WhenAll(tasks);
    }
    [Benchmark]
    public async Task Original()
    {
        await _formatter.FormatAsync(_bigResult, NullStream.Instance, CancellationToken.None);
    }

    [Benchmark]
    public async Task Pipewriter()
    {
        await _formatter.PipeWriter(_bigResult, NullStream.Instance, CancellationToken.None);
    }

    [Benchmark]
    public async Task Original_Parallel()
    {
        var tasks = new Task[10];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = _formatter
                .FormatAsync(_bigResult, NullStream.Instance, CancellationToken.None)
                .AsTask();
        }

        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task Pipewriter_Parallel()
    {
        var tasks = new Task[10];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = _formatter
                .PipeWriter(_bigResult, NullStream.Instance, CancellationToken.None)
                .AsTask();
        }

        await Task.WhenAll(tasks);
    }

    public static IQueryResult CreateBigResult()
    {
        var longString = new string('a', 1000);
        var shortString = new string('a', 10);
        var longBytes = Enumerable.Repeat((byte)1, 100_000).ToArray();
        var longBase64 = Convert.ToBase64String(longBytes);
        var date = new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var time = new TimeSpan(0, 0, 0, 0, 100);
        var byteValue = (byte)1;
        var shortValue = (short)1;
        var intValue = 1;
        var longValue = 1L;
        var ulongValue = 1UL;
        var floatValue = 1.0f;
        var doubleValue = 1.0;
        var decimalValue = 1.0m;
        var boolValue = true;

        var data = new Dictionary<string, object>();
        for (var i = 0; i < 100; i++)
        {
            var nested = new Dictionary<string, object>();
            data.Add(i.ToString(), nested);
            for (var j = 0; j < 100; j++)
            {
                nested[j.ToString()] = (i % 14) switch
                {
                    0 => longString,
                    1 => shortString,
                    2 => longBase64,
                    3 => date,
                    4 => time,
                    5 => byteValue,
                    6 => shortValue,
                    7 => intValue,
                    8 => longValue,
                    9 => ulongValue,
                    10 => floatValue,
                    11 => doubleValue,
                    12 => decimalValue,
                    13 => boolValue,
                    _ => throw new InvalidOperationException()
                };
            }
        }

        return new QueryResult(data);
    }

    public static IQueryResult CreateSmall()
    {
        var longString = new string('a', 100);
        var shortString = new string('a', 10);
        var longBytes = Enumerable.Repeat((byte)1, 100).ToArray();
        var longBase64 = Convert.ToBase64String(longBytes);
        var date = new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var time = new TimeSpan(0, 0, 0, 0, 100);
        var byteValue = (byte)1;
        var shortValue = (short)1;
        var intValue = 1;
        var longValue = 1L;
        var ulongValue = 1UL;
        var floatValue = 1.0f;
        var doubleValue = 1.0;
        var decimalValue = 1.0m;
        var boolValue = true;

        var data = new Dictionary<string, object>();
        for (var i = 0; i < 2; i++)
        {
            var nested = new Dictionary<string, object>();
            data.Add(i.ToString(), nested);
            for (var j = 0; j < 14; j++)
            {
                nested[j.ToString()] = (i % 14) switch
                {
                    0 => longString,
                    1 => shortString,
                    2 => longBase64,
                    3 => date,
                    4 => time,
                    5 => byteValue,
                    6 => shortValue,
                    7 => intValue,
                    8 => longValue,
                    9 => ulongValue,
                    10 => floatValue,
                    11 => doubleValue,
                    12 => decimalValue,
                    13 => boolValue,
                    _ => throw new InvalidOperationException()
                };
            }
        }

        return new QueryResult(data);
    }
}

public sealed class NullStream : Stream
{
    private long _position = 0;

    /// <inheritdoc />
    public override void Flush()
    {
    }

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return Task.Delay(0, cancellationToken);
    }

    /// <inheritdoc />
    public override async Task WriteAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        await Task.Delay(0, cancellationToken);
        await base.WriteAsync(buffer, offset, count, cancellationToken);
    }

    /// <inheritdoc />
    public override async ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = new CancellationToken())
    {
        await Task.Delay(0, cancellationToken);
        await base.WriteAsync(buffer, cancellationToken);
    }

    /// <inheritdoc />
    public override async Task CopyToAsync(
        Stream destination,
        int bufferSize,
        CancellationToken cancellationToken)
    {
        await Task.Delay(0, cancellationToken);
        await base.CopyToAsync(destination, bufferSize, cancellationToken);
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    public override void SetLength(long value)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        _position += count;
    }

    /// <inheritdoc />
    public override bool CanRead => false;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => true;

    /// <inheritdoc />
    public override long Length => _position;

    /// <inheritdoc />
    public override long Position
    {
        get => _position;
        set => throw new System.NotImplementedException();
    }

    public static readonly Stream Instance = new NullStream();
}

/// <summary>
/// A <see cref="IBufferWriter{T}"/> that writes to a rented buffer.
/// </summary>
internal sealed class ArrayWriter : IBufferWriter<byte>, IDisposable
{
    private const int _initialBufferSize = 512;
    private byte[] _buffer;
    private int _capacity;
    private int _start;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayWriter"/> class.
    /// </summary>
    public ArrayWriter()
    {
        _buffer = ArrayPool<byte>.Shared.Rent(_initialBufferSize);
        _capacity = _buffer.Length;
        _start = 0;
    }

    /// <summary>
    /// Gets the number of bytes written to the buffer.
    /// </summary>
    public int Length => _start;

    /// <summary>
    /// Gets the underlying buffer.
    /// </summary>
    /// <returns>
    /// The underlying buffer.
    /// </returns>
    /// <remarks>
    /// Accessing the underlying buffer directly is not recommended.
    /// If possible use <see cref="GetWrittenMemory"/> or <see cref="GetWrittenSpan"/>.
    /// </remarks>
    public byte[] GetInternalBuffer() => _buffer;

    /// <summary>
    /// Gets the part of the buffer that has been written to.
    /// </summary>
    /// <returns>
    /// A <see cref="ReadOnlyMemory{T}"/> of the written portion of the buffer.
    /// </returns>
    public ReadOnlyMemory<byte> GetWrittenMemory()
        => _buffer.AsMemory().Slice(0, _start);

    /// <summary>
    /// Gets the part of the buffer that has been written to.
    /// </summary>
    /// <returns>
    /// A <see cref="ReadOnlySpan{T}"/> of the written portion of the buffer.
    /// </returns>
    public ReadOnlySpan<byte> GetWrittenSpan()
        => _buffer.AsSpan().Slice(0, _start);

    /// <summary>
    /// Advances the writer by the specified number of bytes.
    /// </summary>
    /// <param name="count">
    /// The number of bytes to advance the writer by.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="count"/> is negative or
    /// if <paramref name="count"/> is greater than the
    /// available capacity on the internal buffer.
    /// </exception>
    public void Advance(int count)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ArrayWriter));
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (count > _capacity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                count,
                "asd");
        }

        _start += count;
        _capacity -= count;
    }

    /// <summary>
    /// Gets a <see cref="Memory{T}"/> to write to.
    /// </summary>
    /// <param name="sizeHint">
    /// The minimum size of the returned <see cref="Memory{T}"/>.
    /// </param>
    /// <returns>
    /// A <see cref="Memory{T}"/> to write to.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="sizeHint"/> is negative.
    /// </exception>
    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ArrayWriter));
        }

        if (sizeHint < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeHint));
        }

        var size = sizeHint < 1
            ? _initialBufferSize
            : sizeHint;
        EnsureBufferCapacity(size);
        return _buffer.AsMemory().Slice(_start, size);
    }

    /// <summary>
    /// Gets a <see cref="Span{T}"/> to write to.
    /// </summary>
    /// <param name="sizeHint">
    /// The minimum size of the returned <see cref="Span{T}"/>.
    /// </param>
    /// <returns>
    /// A <see cref="Span{T}"/> to write to.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="sizeHint"/> is negative.
    /// </exception>
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ArrayWriter));
        }

        if (sizeHint < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeHint));
        }

        var size = sizeHint < 1
            ? _initialBufferSize
            : sizeHint;
        EnsureBufferCapacity(size);
        return _buffer.AsSpan().Slice(_start, size);
    }

    /// <summary>
    /// Gets the buffer as an <see cref="ArraySegment{T}"/>
    /// </summary>
    /// <returns></returns>
    public ArraySegment<byte> ToArraySegment() => new(_buffer, 0, _start);

    /// <summary>
    /// Ensures that the internal buffer has the needed capacity.
    /// </summary>
    /// <param name="neededCapacity">
    /// The needed capacity on the internal buffer.
    /// </param>
    private void EnsureBufferCapacity(int neededCapacity)
    {
        // check if we have enough capacity available on the buffer.
        if (_capacity < neededCapacity)
        {
            // if we need to expand the buffer we first capture the original buffer.
            var buffer = _buffer;

            // next we determine the new size of the buffer, we at least double the size to avoid
            // expanding the buffer too often.
            var newSize = buffer.Length * 2;

            // if that new buffer size is not enough to satisfy the needed capacity
            // we add the needed capacity to the doubled buffer capacity.
            if (neededCapacity > newSize - _start)
            {
                newSize += neededCapacity;
            }

            // next we will rent a new array from the array pool that supports
            // the new capacity requirements.
            _buffer = ArrayPool<byte>.Shared.Rent(newSize);

            // the rented array might have a larger size than the needed capacity,
            // so we will take the buffer length and calculate from that the free capacity.
            _capacity += _buffer.Length - buffer.Length;

            // finally we copy the data from the original buffer to the new buffer.
            buffer.AsSpan().CopyTo(_buffer);

            // last but not least we return the original buffer to the array pool.
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public void Reset() => _start = 0;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = Array.Empty<byte>();
            _capacity = 0;
            _start = 0;
            _disposed = true;
        }
    }
}

/// <summary>
/// The default JSON formatter for <see cref="IQueryResult"/>.
/// </summary>
public sealed class JsonResultFormatter : IQueryResultFormatter, IExecutionResultFormatter
{
    private static ReadOnlySpan<byte> Data => "data"u8;

    private static ReadOnlySpan<byte> Items => "items"u8;

    private static ReadOnlySpan<byte> Errors => "errors"u8;

    private static ReadOnlySpan<byte> Extensions => "extensions"u8;

    private static ReadOnlySpan<byte> Message => "message"u8;

    private static ReadOnlySpan<byte> Locations => "locations"u8;

    private static ReadOnlySpan<byte> Path => "path"u8;

    private static ReadOnlySpan<byte> Line => "line"u8;

    private static ReadOnlySpan<byte> Column => "column"u8;

    private static ReadOnlySpan<byte> Incremental => "incremental"u8;

    private readonly JsonWriterOptions _options;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly bool _stripNullProps;
    private readonly bool _stripNullElements;

    /// <summary>
    /// Initializes a new instance of <see cref="JsonResultFormatter"/>.
    /// </summary>
    /// <param name="options">
    /// The JSON result formatter options
    /// </param>
    public JsonResultFormatter(JsonResultFormatterOptions options = default)
    {
        _options = options.CreateWriterOptions();
        _serializerOptions = options.CreateSerializerOptions();
        _stripNullProps = false;
        _stripNullElements = false;
    }

    /// <inheritdoc cref="IExecutionResultFormatter.FormatAsync"/>
    public async ValueTask FormatAsync(
        IExecutionResult result,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        if (result.Kind is ExecutionResultKind.SingleResult)
        {
            await FormatAsync((IQueryResult)result, outputStream, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Formats a query result as JSON string.
    /// </summary>
    /// <param name="result">
    /// The query result.
    /// </param>
    /// <returns>
    /// Returns the JSON string.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> is <c>null</c>.
    /// </exception>
    public unsafe string Format(IQueryResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        using var buffer = new ArrayWriter();

        Format(result, buffer);

        fixed (byte* b = buffer.GetInternalBuffer())
        {
            return Encoding.UTF8.GetString(b, buffer.Length);
        }
    }

    /// <summary>
    /// Formats a query result as JSON string.
    /// </summary>
    /// <param name="result">
    /// The query result.
    /// </param>
    /// <param name="writer">
    /// The JSON writer.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> is <c>null</c>.
    /// <paramref name="writer"/> is <c>null</c>.
    /// </exception>
    public void Format(IQueryResult result, Utf8JsonWriter writer)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        WriteResult(writer, result);
    }

    /// <summary>
    /// Formats a <see cref="HotChocolate.IError"/> as JSON string.
    /// </summary>
    /// <param name="error">
    /// The error object.
    /// </param>
    /// <param name="writer">
    /// The JSON writer.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// <paramref name="writer"/> is <c>null</c>.
    /// </exception>
    public void FormatError(IError error, Utf8JsonWriter writer)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        WriteError(writer, error);
    }

    /// <summary>
    /// Formats a list of <see cref="IError"/>s as JSON array string.
    /// </summary>
    /// <param name="errors">
    /// The list of error objects.
    /// </param>
    /// <param name="writer">
    /// The JSON writer.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// <paramref name="writer"/> is <c>null</c>.
    /// </exception>
    public void FormatErrors(IReadOnlyList<IError> errors, Utf8JsonWriter writer)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        writer.WriteStartArray();

        for (var i = 0; i < errors.Count; i++)
        {
            WriteError(writer, errors[i]);
        }

        writer.WriteEndArray();
    }

    /// <inheritdoc cref="IQueryResultFormatter.Format"/>
    public void Format(IQueryResult result, IBufferWriter<byte> writer)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        using var jsonWriter = new Utf8JsonWriter(writer, _options);
        WriteResult(jsonWriter, result);
        jsonWriter.Flush();
    }

    /// <inheritdoc cref="IQueryResultFormatter.FormatAsync"/>
    public ValueTask FormatAsync(
        IQueryResult result,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (outputStream is null)
        {
            throw new ArgumentNullException(nameof(outputStream));
        }

        return FormatInternalAsync(result, outputStream, cancellationToken);
    }

    public ValueTask PipeWriter(
        IQueryResult result,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (outputStream is null)
        {
            throw new ArgumentNullException(nameof(outputStream));
        }

        var pipe = new Pipe();
        var writer = pipe.Writer;
        var reader = pipe.Reader;

        var task = reader.CopyToAsync(outputStream, cancellationToken);
        Format(result, writer);

        writer.Complete();

        return new ValueTask(task);
    }

    private async ValueTask FormatInternalAsync(
        IQueryResult result,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        using var buffer = new ArrayWriter();

        Format(result, buffer);

        await outputStream
            .WriteAsync(buffer.GetWrittenMemory(), cancellationToken)
            .ConfigureAwait(false);

        await outputStream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private void WriteResult(Utf8JsonWriter writer, IQueryResult result)
    {
        writer.WriteStartObject();

        WriteErrors(writer, result.Errors);
        WriteData(writer, result.Data);
        WriteItems(writer, result.Items);
        WriteIncremental(writer, result.Incremental);
        WriteExtensions(writer, result.Extensions);
        WritePatchInfo(writer, result);
        WriteHasNext(writer, result);

        writer.WriteEndObject();
    }

    private static void WritePatchInfo(
        Utf8JsonWriter writer,
        IQueryResult result)
    {
        if (result.Label is not null)
        {
            writer.WriteString("label", result.Label);
        }

        if (result.Path is not null)
        {
        }
    }

    private static void WriteHasNext(
        Utf8JsonWriter writer,
        IQueryResult result)
    {
        if (result.HasNext.HasValue)
        {
            writer.WriteBoolean("hasNext", result.HasNext.Value);
        }
    }

    private void WriteData(
        Utf8JsonWriter writer,
        IReadOnlyDictionary<string, object?>? data)
    {
        if (data is not null)
        {
            writer.WritePropertyName(Data);

            if (data is ObjectResult resultMap)
            {
                WriteObjectResult(writer, resultMap);
            }
            else
            {
                WriteDictionary(writer, data);
            }
        }
    }

    private void WriteItems(Utf8JsonWriter writer, IReadOnlyList<object?>? items)
    {
        if (items is { Count: > 0 })
        {
            writer.WritePropertyName(Items);

            writer.WriteStartArray();

            for (var i = 0; i < items.Count; i++)
            {
                WriteFieldValue(writer, items[i]);
            }

            writer.WriteEndArray();
        }
    }

    private void WriteErrors(Utf8JsonWriter writer, IReadOnlyList<IError>? errors)
    {
        if (errors is { Count: > 0 })
        {
            writer.WritePropertyName(Errors);

            writer.WriteStartArray();

            for (var i = 0; i < errors.Count; i++)
            {
                WriteError(writer, errors[i]);
            }

            writer.WriteEndArray();
        }
    }

    private void WriteError(Utf8JsonWriter writer, IError error)
    {
        writer.WriteStartObject();

        writer.WriteString(Message, error.Message);

        WriteLocations(writer, error.Locations);

        WriteExtensions(writer, error.Extensions);

        writer.WriteEndObject();
    }

    private static void WriteLocations(Utf8JsonWriter writer, IReadOnlyList<Location>? locations)
    {
        if (locations is { Count: > 0 })
        {
            writer.WritePropertyName(Locations);

            writer.WriteStartArray();

            for (var i = 0; i < locations.Count; i++)
            {
                WriteLocation(writer, locations[i]);
            }

            writer.WriteEndArray();
        }
    }

    private static void WriteLocation(Utf8JsonWriter writer, Location location)
    {
        writer.WriteStartObject();
        writer.WriteNumber(Line, location.Line);
        writer.WriteNumber(Column, location.Column);
        writer.WriteEndObject();
    }

    private void WriteExtensions(
        Utf8JsonWriter writer,
        IReadOnlyDictionary<string, object?>? dict)
    {
        if (dict is { Count: > 0 })
        {
            writer.WritePropertyName(Extensions);
            WriteDictionary(writer, dict);
        }
    }

    private void WriteIncremental(Utf8JsonWriter writer, IReadOnlyList<IQueryResult>? patches)
    {
        if (patches is { Count: > 0 })
        {
            writer.WritePropertyName(Incremental);

            writer.WriteStartArray();

            for (var i = 0; i < patches.Count; i++)
            {
                WriteResult(writer, patches[i]);
            }

            writer.WriteEndArray();
        }
    }

    private void WriteDictionary(
        Utf8JsonWriter writer,
        IReadOnlyDictionary<string, object?> dict)
    {
        writer.WriteStartObject();

        foreach (var item in dict)
        {
            if (item.Value is null && _stripNullProps)
            {
                continue;
            }

            writer.WritePropertyName(item.Key);
            WriteFieldValue(writer, item.Value);
        }

        writer.WriteEndObject();
    }

    private void WriteDictionary(
        Utf8JsonWriter writer,
        Dictionary<string, object?> dict)
    {
        writer.WriteStartObject();

        foreach (var item in dict)
        {
            if (item.Value is null && _stripNullProps)
            {
                continue;
            }

            writer.WritePropertyName(item.Key);
            WriteFieldValue(writer, item.Value);
        }

        writer.WriteEndObject();
    }

    private void WriteObjectResult(
        Utf8JsonWriter writer,
        ObjectResult objectResult)
    {
        writer.WriteStartObject();

        ref var searchSpace = ref objectResult.GetReference();

        for (var i = 0; i < objectResult.Capacity; i++)
        {
            var field = Unsafe.Add(ref searchSpace, i);

            if (!field.IsInitialized || (field.Value is null && _stripNullProps))
            {
                continue;
            }

            writer.WritePropertyName(field.Name);
            WriteFieldValue(writer, field.Value);
        }

        writer.WriteEndObject();
    }

    private void WriteListResult(
        Utf8JsonWriter writer,
        ListResult list)
    {
        writer.WriteStartArray();

        ref var searchSpace = ref list.GetReference();

        for (var i = 0; i < list.Count; i++)
        {
            var element = Unsafe.Add(ref searchSpace, i);

            if (element is null && _stripNullElements)
            {
                continue;
            }

            WriteFieldValue(writer, element);
        }

        writer.WriteEndArray();
    }

    private void WriteList(
        Utf8JsonWriter writer,
        IList list)
    {
        writer.WriteStartArray();

        for (var i = 0; i < list.Count; i++)
        {
            var element = list[i];

            if (element is null && _stripNullElements)
            {
                continue;
            }

            WriteFieldValue(writer, element);
        }

        writer.WriteEndArray();
    }

#if NET5_0_OR_GREATER
    private void WriteJsonElement(
        Utf8JsonWriter writer,
        JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                WriteJsonObject(writer, element);
                break;

            case JsonValueKind.Array:
                WriteJsonArray(writer, element);
                break;

            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;

            case JsonValueKind.Number:
                writer.WriteRawValue(element.GetRawText());
                break;

            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;

            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;

            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void WriteJsonObject(
        Utf8JsonWriter writer,
        JsonElement element)
    {
        writer.WriteStartObject();

        foreach (var item in element.EnumerateObject())
        {
            if (item.Value.ValueKind is JsonValueKind.Null && _stripNullProps)
            {
                continue;
            }

            writer.WritePropertyName(item.Name);
            WriteJsonElement(writer, item.Value);
        }

        writer.WriteEndObject();
    }

    private void WriteJsonArray(
        Utf8JsonWriter writer,
        JsonElement element)
    {
        writer.WriteStartArray();

        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind is JsonValueKind.Null && _stripNullElements)
            {
                continue;
            }

            WriteJsonElement(writer, item);
        }

        writer.WriteEndArray();
    }

#endif
    private void WriteFieldValue(
        Utf8JsonWriter writer,
        object? value)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        switch (value)
        {
            case ObjectResult resultMap:
                WriteObjectResult(writer, resultMap);
                break;

            case ListResult resultMapList:
                WriteListResult(writer, resultMapList);
                break;

#if NET6_0_OR_GREATER
            case JsonDocument doc:
                WriteJsonElement(writer, doc.RootElement);
                break;

            case JsonElement element:
                WriteJsonElement(writer, element);
                break;

            case RawJsonValue rawJsonValue:
                writer.WriteRawValue(rawJsonValue.Value.Span, true);
                break;
#endif
            case NeedsFormatting unformatted:
                unformatted.FormatValue(writer, _serializerOptions);
                break;

            case Dictionary<string, object?> dict:
                WriteDictionary(writer, dict);
                break;

            case IReadOnlyDictionary<string, object?> dict:
                WriteDictionary(writer, dict);
                break;

            case IList list:
                WriteList(writer, list);
                break;

            case IError error:
                WriteError(writer, error);
                break;

            case string s:
                writer.WriteStringValue(s);
                break;

            case byte b:
                writer.WriteNumberValue(b);
                break;

            case short s:
                writer.WriteNumberValue(s);
                break;

            case ushort s:
                writer.WriteNumberValue(s);
                break;

            case int i:
                writer.WriteNumberValue(i);
                break;

            case uint i:
                writer.WriteNumberValue(i);
                break;

            case long l:
                writer.WriteNumberValue(l);
                break;

            case ulong l:
                writer.WriteNumberValue(l);
                break;

            case float f:
                writer.WriteNumberValue(f);
                break;

            case double d:
                writer.WriteNumberValue(d);
                break;

            case decimal d:
                writer.WriteNumberValue(d);
                break;

            case bool b:
                writer.WriteBooleanValue(b);
                break;

            case Uri u:
                writer.WriteStringValue(u.ToString());
                break;

            default:
                writer.WriteStringValue(value.ToString());
                break;
        }
    }
}
