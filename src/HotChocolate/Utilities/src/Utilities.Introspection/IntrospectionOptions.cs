using System;
using System.Net.Http;
using HotChocolate.Transport.Http;
using static HotChocolate.Utilities.Introspection.Properties.IntroResources;

namespace HotChocolate.Utilities.Introspection;

public struct IntrospectionOptions : IEquatable<IntrospectionOptions>
{
    private GraphQLHttpMethod? _method;
    private int? _typeDepth;

    /// <summary>
    /// Gets or sets the HTTP method.
    /// </summary>
    public GraphQLHttpMethod Method
    {
        get => _method ??= GraphQLHttpMethod.Post;
        set => _method = value;
    }

    /// <summary>
    /// Gets or sets the GraphQL request <see cref="Uri"/>.
    /// </summary>
    public Uri? Uri { get; set; }

    /// <summary>
    /// Gets or sets a hook that can alter the <see cref="HttpRequestMessage"/> before it is sent.
    /// </summary>
    public OnHttpRequestMessageCreated? OnMessageCreated { get; set; }

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

    public override bool Equals(object obj)
    {
        throw new NotImplementedException();
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }

    public static bool operator ==(IntrospectionOptions left, IntrospectionOptions right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(IntrospectionOptions left, IntrospectionOptions right)
    {
        return !(left == right);
    }

    public bool Equals(IntrospectionOptions other)
    {
        throw new NotImplementedException();
    }
}