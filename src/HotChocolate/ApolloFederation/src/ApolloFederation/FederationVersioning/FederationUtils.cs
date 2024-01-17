using System.Collections.Generic;
using System.Diagnostics;

namespace HotChocolate.ApolloFederation;

internal readonly struct FederationVersionValues<T>
{
    public required T[] Values { get; init; }

    public bool IsValidVersion(FederationVersion v)
    {
        return (int)v > 0 && (int)v < Values.Length;
    }

    public ref T this[FederationVersion v]
    {
        get => ref Values[(int)v];
    }

    public ref T V20 => ref this[FederationVersion.Federation20];
    public ref T V21 => ref this[FederationVersion.Federation21];
    public ref T V22 => ref this[FederationVersion.Federation22];
    public ref T V23 => ref this[FederationVersion.Federation23];
    public ref T V24 => ref this[FederationVersion.Federation24];
    public ref T V25 => ref this[FederationVersion.Federation25];
    public ref T V26 => ref this[FederationVersion.Federation26];

    public bool AllSet
    {
        get
        {
            foreach (var v in Values)
            {
                if (v == null)
                {
                    return false;
                }
            }
            return true;
        }
    }
}

/// <summary>
/// Utility class to help calculate different Apollo Federation @link imports based on the supported version.
/// </summary>
internal sealed class FederationUtils
{
    private const string _federationSpecBaseUrl = "https://specs.apollo.dev/federation/v";

    private static readonly FederationVersionValues<List<string>> _imports;
    private static readonly FederationVersionValues<string> _urls;

    private static FederationVersionValues<T> CreateFederationValues<T>()
    {
        return new()
        {
            Values = new T[(int)FederationVersion.Count],
        };
    }

    static FederationUtils()
    {
        _imports = CreateFederationValues<List<string>>();
        _imports.V20 = new()
        {
            "@extends",
            "@external",
            "@key",
            "@inaccessible",
            "@override",
            "@provides",
            "@requires",
            "@shareable",
            "@tag",
            "FieldSet",
        };
        _imports.V21 = new(_imports.V20);
        _imports.V21.Add("@composeDirective");

        _imports.V22 = _imports.V21;

        _imports.V23 = new(_imports.V22);
        _imports.V23.Add("@interfaceObject");

        _imports.V24 = _imports.V23;

        _imports.V25 = new(_imports.V24);
        _imports.V25.Add("@authenticated");
        _imports.V25.Add("@requiresPolicy");

        _imports.V26 = new(_imports.V25);
        _imports.V26.Add("@policy");

        Debug.Assert(_imports.AllSet);

        _urls = CreateFederationValues<string>();
        for (FederationVersion i = 0; i < FederationVersion.Count; i++)
        {
            _urls[i] = $"{_federationSpecBaseUrl}2.{(int)i}";
        }

        Debug.Assert(_urls.AllSet);
    }

    /// <summary>
    /// Retrieve Apollo Federation @link information corresponding to the specified version.
    /// </summary>
    /// <param name="federationVersion">
    /// Supported Apollo Federation version
    /// </param>
    /// <returns>
    /// Federation @link information corresponding to the specified version.
    /// </returns>
    internal static LinkDirective GetFederationLink(FederationVersion federationVersion)
    {
        if (!_imports.IsValidVersion(federationVersion))
        {
            throw ThrowHelper.FederationVersion_Unknown(federationVersion);
        }

        return new LinkDirective(
            _urls[federationVersion],
            _imports[federationVersion]);
    }
}

