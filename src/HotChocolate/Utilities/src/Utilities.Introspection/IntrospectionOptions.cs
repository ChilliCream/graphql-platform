using HotChocolate.Transport.Http;
using static HotChocolate.Utilities.Introspection.Properties.IntroResources;

namespace HotChocolate.Utilities.Introspection;

/// <summary>
/// Represents the GraphQL introspection options.
/// </summary>
public struct IntrospectionOptions : IEquatable<IntrospectionOptions>
{
    private GraphQLHttpMethod? _method;
    private int? _typeDepth;

    /// <summary>
    /// Gets or sets the HTTP method that shall be used for the introspection request.
    /// </summary>
    public GraphQLHttpMethod Method
    {
        get => _method ??= GraphQLHttpMethod.Post;
        set => _method = value;
    }

    /// <summary>
    /// Gets or sets the GraphQL server <see cref="Uri"/>.
    /// </summary>
    public Uri? Uri { get; set; }

    /// <summary>
    /// Gets or sets a hook that can alter the <see cref="HttpRequestMessage"/> before it is sent.
    /// </summary>
    public OnHttpRequestMessageCreated? OnMessageCreated { get; set; }

    /// <summary>
    /// Gets or sets a value that determines how deep the introspection request shall dive into types.
    /// </summary>
    public int TypeDepth
    {
        get => _typeDepth ??= 6;
        set
        {
            if (value < 3)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(TypeDepth),
                    value,
                    IntrospectionOptions_MinTypeDepth);
            }

            _typeDepth = value;
        }
    }

    public bool Equals(IntrospectionOptions other)
        => Equals(_method, other._method)
            && Equals(_typeDepth, other._typeDepth)
            && Equals(Uri, other.Uri)
            && ReferenceEquals(OnMessageCreated, other.OnMessageCreated);

    public override bool Equals(object? obj)
        => obj is IntrospectionOptions options && Equals(options);

    public override int GetHashCode()
        => HashCode.Combine(_method, _typeDepth, Uri, OnMessageCreated);

    public static bool operator ==(IntrospectionOptions left, IntrospectionOptions right)
        => left.Equals(right);

    public static bool operator !=(IntrospectionOptions left, IntrospectionOptions right)
        => !(left == right);
}
