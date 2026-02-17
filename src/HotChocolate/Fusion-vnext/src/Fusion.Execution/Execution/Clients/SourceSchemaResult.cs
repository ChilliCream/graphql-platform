using HotChocolate.Fusion.Text.Json;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class SourceSchemaResult : IDisposable
{
    private static ReadOnlySpan<byte> DataProperty => "data"u8;
    private static ReadOnlySpan<byte> ErrorsProperty => "errors"u8;
    private static ReadOnlySpan<byte> ExtensionsProperty => "extensions"u8;
    private readonly SourceResultDocument _document;
    private readonly bool _ownsDocument;

    public SourceSchemaResult(
        Path path,
        SourceResultDocument document,
        FinalMessage final = FinalMessage.Undefined)
        : this(path, document, final, ownsDocument: true)
    {
    }

    private SourceSchemaResult(
        Path path,
        SourceResultDocument document,
        FinalMessage final,
        bool ownsDocument)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(document);

        _document = document;
        _ownsDocument = ownsDocument;
        Path = path;
        Final = final;
    }

    public Path Path { get; }

    public SourceResultElement Data
    {
        get
        {
            _document.Root.TryGetProperty(DataProperty, out var data);
            return data;
        }
    }

    public SourceSchemaErrors? Errors
        => _document.Root.TryGetProperty(ErrorsProperty, out var errors)
            ? SourceSchemaErrors.From(errors)
            : null;

    internal SourceResultElement RawErrors
    {
        get
        {
            _document.Root.TryGetProperty(ErrorsProperty, out var errors);
            return errors;
        }
    }

    public bool HasErrors => _document.Root.TryGetProperty(ErrorsProperty, out _);

    public SourceResultElement Extensions
    {
        get
        {
            _document.Root.TryGetProperty(ExtensionsProperty, out var extensions);
            return extensions;
        }
    }

    public FinalMessage Final { get; }

    internal SourceSchemaResult WithPath(Path path) => new(path, _document, Final, ownsDocument: false);

    public void Dispose()
    {
        if (_ownsDocument)
        {
            _document.Dispose();
        }
    }
}
