using System.Collections;
using System.Collections.Immutable;
using System.Security.Cryptography;
using StrawberryShake.Internal;
using StrawberryShake.Json;

namespace StrawberryShake;

/// <summary>
/// Represents an operation request that is send to the GraphQL server.
/// </summary>
public sealed class OperationRequest : IEquatable<OperationRequest>
{
    private Dictionary<string, object?>? _extensions;
    private Dictionary<string, object?>? _contextData;
    private string? _hash;

    /// <summary>
    /// Creates a new instance of <see cref="OperationRequest"/>.
    /// </summary>
    /// <param name="name">The operation name.</param>
    /// <param name="document">The GraphQL query document containing this operation.</param>
    /// <param name="variables">The request variable values.</param>
    /// <param name="strategy">The request strategy to the connection.</param>
    /// <param name="files">The files of this request</param>
    public OperationRequest(
        string name,
        IDocument document,
        IReadOnlyDictionary<string, object?>? variables = null,
        IReadOnlyDictionary<string, Upload?>? files = null,
        RequestStrategy strategy = RequestStrategy.Default)
        : this(null, name, document, variables, files,  strategy)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="OperationRequest"/>.
    /// </summary>
    /// <param name="id">The the optional request id.</param>
    /// <param name="name">The operation name.</param>
    /// <param name="document">The GraphQL query document containing this operation.</param>
    /// <param name="variables">The request variable values.</param>
    /// <param name="strategy">The request strategy to the connection.</param>
    /// <param name="files">The files of this request</param>
    public OperationRequest(
        string? id,
        string name,
        IDocument document,
        IReadOnlyDictionary<string, object?>? variables = null,
        IReadOnlyDictionary<string, Upload?>? files = null,
        RequestStrategy strategy = RequestStrategy.Default)
    {
        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Document = document ?? throw new ArgumentNullException(nameof(document));
        Variables = variables ?? ImmutableDictionary<string, object?>.Empty;
        Files = files ?? ImmutableDictionary<string, Upload?>.Empty;
        Strategy = strategy;
    }

    /// <summary>
    /// Deconstructs <see cref="OperationRequest"/>.
    /// </summary>
    /// <param name="id">The the optional request id.</param>
    /// <param name="name">The operation name.</param>
    /// <param name="document">The GraphQL query document containing this operation.</param>
    /// <param name="variables">The request variable values.</param>
    /// <param name="extensions">The request extension values.</param>
    /// <param name="contextData">The local context data.</param>
    /// <param name="strategy">The request strategy to the connection.</param>
    /// <param name="files">The files of the request</param>
    public void Deconstruct(
        out string? id,
        out string name,
        out IDocument document,
        out IReadOnlyDictionary<string, object?> variables,
        out IReadOnlyDictionary<string, object?>? extensions,
        out IReadOnlyDictionary<string, object?>? contextData,
        out IReadOnlyDictionary<string, Upload?>? files,
        out RequestStrategy strategy)
    {
        id = Id;
        name = Name;
        document = Document;
        variables = Variables;
        extensions = _extensions;
        contextData = _contextData;
        files = Files;
        strategy = Strategy;
    }

    /// <summary>
    /// Gets the optional request id.
    /// </summary>
    public string? Id { get; }

    /// <summary>
    /// Gets the operation name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the GraphQL query document containing this operation.
    /// </summary>
    public IDocument Document { get; }

    /// <summary>
    /// Gets the request variable values.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Variables { get; }

    /// <summary>
    /// The files of the request
    /// </summary>
    public IReadOnlyDictionary<string, Upload?> Files { get; }

    /// <summary>
    /// Gets the request extension values.
    /// </summary>
    public IDictionary<string, object?> Extensions
    {
        get
        {
            return _extensions ??= new();
        }
    }

    /// <summary>
    /// Gets the local context data.
    /// </summary>
    public IDictionary<string, object?> ContextData
    {
        get
        {
            return _contextData ??= new();
        }
    }

    /// <summary>
    /// Defines the request strategy to the connection.
    /// </summary>
    public RequestStrategy Strategy { get; }

    /// <summary>
    /// Gets the request extension values or null.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? GetExtensionsOrNull() =>
        _extensions;

    /// <summary>
    /// Gets the request context data values or null.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? GetContextDataOrNull() =>
        _contextData;

    public bool Equals(OperationRequest? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Id == other.Id &&
            Name == other.Name &&
            Document.Equals(other.Document) &&
            ComparisonHelper.DictionaryEqual(Variables, other.Variables);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((OperationRequest)obj);
    }

    public string GetHash()
    {
        if (_hash is null)
        {
            using var writer = new ArrayWriter();
            var serializer = new JsonOperationRequestSerializer();
            serializer.Serialize(this, writer, ignoreExtensions: true);

            using var sha256 = SHA256.Create();
            var buffer = sha256.ComputeHash(writer.GetInternalBuffer(), 0, writer.Length);
            _hash = Convert.ToBase64String(buffer);
        }

        return _hash;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash =
                (Id?.GetHashCode() ?? 0) * 397 ^
                Name.GetHashCode() * 397 ^
                Document.GetHashCode() * 397;

            foreach (var variable in Variables)
            {
                if (variable.Value is not string && variable.Value is IEnumerable inner)
                {
                    hash ^= GetHashCodeFromList(inner) * 397;
                }
                else
                {
                    hash ^= variable.GetHashCode();
                }
            }

            return hash;
        }
    }

    private static int GetHashCodeFromList(IEnumerable enumerable)
    {
        var hash = 17;

        foreach (var element in enumerable)
        {
            if (element is not string && element is IEnumerable inner)
            {
                hash ^= GetHashCodeFromList(inner) * 397;
            }
            else if (element is not null)
            {
                hash ^= element.GetHashCode() * 397;
            }
        }

        return hash;
    }
}
