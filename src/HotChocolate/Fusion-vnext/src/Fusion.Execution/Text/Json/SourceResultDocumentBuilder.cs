using System.Text.Json;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Text.Json;

internal sealed class SourceResultDocumentBuilder
{
    public SourceResultElementBuilder Root { get; }

    public SourceResultDocument Build() => throw new NotImplementedException();
}

internal readonly struct SourceResultElementBuilder
{
    public JsonValueKind ValueKind => throw new NotImplementedException();

    public SourceResultElementBuilder SetObjectValue() => throw new NotImplementedException();

    public SourceResultElementBuilder SetListValue(int length) => throw new NotImplementedException();

    public void SetStringValue(ReadOnlySpan<byte> s) => throw new NotImplementedException();

    public void SetStringValue(string s) => throw new NotImplementedException();

    public void SetBooleanValue(bool b) => throw new NotImplementedException();

    public void SetNullValue() => throw new NotImplementedException();

    public SourceResultElementBuilder CreateProperty(Selection selection) => throw new NotImplementedException();

    public IEnumerable<SourceResultElementBuilder> EnumerateArray() => throw new NotImplementedException();
}
