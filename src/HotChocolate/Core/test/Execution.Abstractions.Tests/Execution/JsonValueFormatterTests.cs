using System.Buffers;
using System.Collections;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Text.Json;

namespace HotChocolate.Execution;

public class JsonValueFormatterTests
{
    [Fact]
    public void WriteSByteAsNumber()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new JsonWriter(buffer, new JsonWriterOptions { SkipValidation = true });

        // act
        JsonValueFormatter.WriteValue(writer, (sbyte)2, new JsonSerializerOptions());

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("2", result);
    }

    [Fact]
    public void WriteErrors_Should_PreserveGetterOrder_When_SingleErrorIsCustom()
    {
        // arrange
        var calls = new List<string>();
        var error = new ObservedError(calls) { Message = "failure", Path = Path.Root.Append("field") };
        var errors = ImmutableList.Create<IError>(error);
        using var buffer = new PooledArrayWriter();
        var writer = new JsonWriter(buffer, new JsonWriterOptions());

        // act
        writer.WriteStartObject();
        JsonValueFormatter.WriteErrors(writer, errors, new JsonSerializerOptions());
        writer.WriteEndObject();

        // assert
        Snapshot.Create()
            .Add(Encoding.UTF8.GetString(buffer.WrittenSpan), "Output", MarkdownLanguages.Json)
            .Add(calls, "Getter Calls")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void WriteErrors_Should_UseEnumeration_When_CustomListHasDifferentIndexer()
    {
        // arrange
        var calls = new List<string>();
        var errors = new ObservedErrorList(calls);
        using var buffer = new PooledArrayWriter();
        var writer = new JsonWriter(buffer, new JsonWriterOptions());

        // act
        writer.WriteStartObject();
        JsonValueFormatter.WriteErrors(writer, errors, new JsonSerializerOptions());
        writer.WriteEndObject();

        // assert
        Snapshot.Create()
            .Add(Encoding.UTF8.GetString(buffer.WrittenSpan), "Output", MarkdownLanguages.Json)
            .Add(calls, "List Calls")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void WriteErrors_Should_KeepFailurePosition_When_SingleErrorIsNull()
    {
        // arrange
        var errors = ImmutableList<IError>.Empty.Add(null!);
        using var buffer = new PooledArrayWriter();
        var writer = new JsonWriter(buffer, new JsonWriterOptions());
        writer.WriteStartObject();

        // act
        var exception = Record.Exception(() =>
            JsonValueFormatter.WriteErrors(writer, errors, new JsonSerializerOptions()));

        // assert
        Snapshot.Create()
            .Add(exception?.GetType().Name, "Exception")
            .Add(Encoding.UTF8.GetString(buffer.WrittenSpan), "Written Prefix")
            .MatchMarkdownSnapshot();
    }

    private sealed record ObservedError(List<string> Calls) : Error, IError
    {
        string IError.Message
        {
            get
            {
                Calls.Add("message");
                return Message;
            }
        }

        Path? IError.Path
        {
            get
            {
                Calls.Add("path");
                return Path;
            }
        }
    }

    private sealed class ObservedErrorList(List<string> calls) : IReadOnlyList<IError>
    {
        public int Count => 1;

        public IError this[int index]
        {
            get
            {
                calls.Add("indexer");
                return new Error { Message = "indexed" };
            }
        }

        public IEnumerator<IError> GetEnumerator()
        {
            calls.Add("enumerator");
            yield return new Error { Message = "enumerated" };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
