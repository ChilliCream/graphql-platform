using System.Text;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Configuration;

/// <summary>
/// Builds a developer-facing path that describes how an unresolved type reference
/// was reached during type discovery. The path is composed from runtime (CLR) member
/// names so that the originating field or argument can be located in the source code.
/// </summary>
internal static class TypeInferencePathBuilder
{
    /// <summary>
    /// Builds a path from the root type to the unresolved type reference.
    /// </summary>
    /// <param name="typeRegistry">
    /// The type registry that holds all registered types.
    /// </param>
    /// <param name="unresolved">
    /// The type reference that could not be inferred or resolved.
    /// </param>
    /// <param name="maxSegments">
    /// The maximum number of arrow-separated parts in the path, including the leaf
    /// (and a leading <c>"..."</c> marker when the chain is longer).
    /// </param>
    /// <returns>
    /// A <see cref="TypeInferencePath"/> that can render a short or a namespace-qualified
    /// form, or <c>null</c> when no member edge pointing at the unresolved reference can
    /// be found.
    /// </returns>
    public static TypeInferencePath? Build(
        TypeRegistry typeRegistry,
        TypeReference unresolved,
        int maxSegments = 5)
    {
        ArgumentNullException.ThrowIfNull(typeRegistry);
        ArgumentNullException.ThrowIfNull(unresolved);

        // the leaf is captured as a runtime type when possible so that it can be
        // rendered with or without its namespace. otherwise the reference text is used.
        var leafType = unresolved is ExtendedTypeReference extendedRef
            ? extendedRef.Type.Type
            : null;
        var leafText = leafType is null ? unresolved.ToString() : null;

        // the first hop locates the member whose type is the unresolved reference.
        if (!TryFindOwner(typeRegistry, unresolved, out var owner, out var segment))
        {
            return null;
        }

        // the path is built leaf-first and reversed before joining. the leaf counts
        // towards the cap, as does a leading "..." marker when the chain is longer.
        var segments = new List<TypeInferenceSegment> { segment };
        var visited = new HashSet<RegisteredType> { owner };
        var truncated = false;

        // the remaining hops walk towards the root by locating the member whose
        // type resolves to the current owner.
        while (TryFindParent(typeRegistry, owner, visited, out var parent, out var parentSegment))
        {
            // adding this ancestor would fill the last slot. if yet another ancestor
            // exists beyond it, that slot is reserved for the leading "..." marker.
            if (segments.Count + 1 >= maxSegments - 1)
            {
                visited.Add(parent);
                truncated = TryFindParent(typeRegistry, parent, visited, out _, out _);

                if (!truncated)
                {
                    segments.Add(parentSegment);
                }

                break;
            }

            segments.Add(parentSegment);
            visited.Add(parent);
            owner = parent;
        }

        segments.Reverse();

        return new TypeInferencePath(leafType, leafText, segments, truncated);
    }

    private static bool TryFindOwner(
        TypeRegistry typeRegistry,
        TypeReference unresolved,
        out RegisteredType owner,
        out TypeInferenceSegment segment)
    {
        bool Matches(TypeReference reference) => reference.Equals(unresolved);

        foreach (var registeredType in typeRegistry.Types)
        {
            if (TryGetMemberSegment(registeredType, Matches, out segment))
            {
                owner = registeredType;
                return true;
            }
        }

        owner = null!;
        segment = default;
        return false;
    }

    private static bool TryFindParent(
        TypeRegistry typeRegistry,
        RegisteredType target,
        HashSet<RegisteredType> visited,
        out RegisteredType parent,
        out TypeInferenceSegment segment)
    {
        bool Matches(TypeReference reference)
            => typeRegistry.TryGetType(reference, out var rt) && ReferenceEquals(rt, target);

        foreach (var registeredType in typeRegistry.Types)
        {
            if (visited.Contains(registeredType))
            {
                continue;
            }

            if (TryGetMemberSegment(registeredType, Matches, out segment))
            {
                parent = registeredType;
                return true;
            }
        }

        parent = null!;
        segment = default;
        return false;
    }

    private static bool TryGetMemberSegment(
        RegisteredType registeredType,
        Func<TypeReference, bool> matches,
        out TypeInferenceSegment segment)
    {
        var owner = registeredType.RuntimeType;

        // only object, interface and input object types expose member edges with a
        // populated configuration. any other kind yields no member segment.
        switch (registeredType.Type)
        {
            case ObjectType { Configuration: { } config }:
                foreach (var field in config.Fields)
                {
                    var memberName = field.Member?.Name ?? field.Name;
                    if (TryMatchOutputField(owner, memberName, field, matches, out segment))
                    {
                        return true;
                    }
                }
                break;

            case InterfaceType { Configuration: { } config }:
                foreach (var field in config.Fields)
                {
                    var memberName = field.Member?.Name ?? field.Name;
                    if (TryMatchOutputField(owner, memberName, field, matches, out segment))
                    {
                        return true;
                    }
                }
                break;

            case InputObjectType { Configuration: { } config }:
                foreach (var field in config.Fields)
                {
                    if (field.Type is { } fieldType && matches(fieldType))
                    {
                        var memberName = field.Property?.Name ?? field.Name;
                        segment = new TypeInferenceSegment(owner, $".{memberName}");
                        return true;
                    }
                }
                break;
        }

        segment = default;
        return false;
    }

    private static bool TryMatchOutputField(
        Type owner,
        string memberName,
        OutputFieldConfiguration field,
        Func<TypeReference, bool> matches,
        out TypeInferenceSegment segment)
    {
        if (field.Type is { } fieldType && matches(fieldType))
        {
            segment = new TypeInferenceSegment(owner, $".{memberName}");
            return true;
        }

        if (field.HasArguments)
        {
            foreach (var argument in field.GetArguments())
            {
                if (argument.Type is { } argType && matches(argType))
                {
                    var argName = argument.Parameter?.Name ?? argument.Name;
                    segment = new TypeInferenceSegment(owner, $".{memberName}({argName})");
                    return true;
                }
            }
        }

        segment = default;
        return false;
    }

    internal static string GetFriendlyTypeName(Type type, bool includeNamespace)
    {
        if (TryGetAlias(type, out var alias))
        {
            return alias;
        }

        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            return elementType is null
                ? type.Name
                : $"{GetFriendlyTypeName(elementType, includeNamespace)}[]";
        }

        if (type.IsByRef)
        {
            var elementType = type.GetElementType();
            return elementType is null
                ? type.Name
                : $"{GetFriendlyTypeName(elementType, includeNamespace)}&";
        }

        if (type.IsPointer)
        {
            var elementType = type.GetElementType();
            return elementType is null
                ? type.Name
                : $"{GetFriendlyTypeName(elementType, includeNamespace)}*";
        }

        if (type.IsGenericType)
        {
            var name = type.Name;
            var backtick = name.IndexOf('`');
            if (backtick > 0)
            {
                name = name[..backtick];
            }

            if (includeNamespace && !string.IsNullOrEmpty(type.Namespace))
            {
                name = $"{type.Namespace}.{name}";
            }

            var arguments = type.GetGenericArguments();
            var builder = new StringBuilder(name);
            builder.Append('<');

            for (var i = 0; i < arguments.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(GetFriendlyTypeName(arguments[i], includeNamespace));
            }

            builder.Append('>');
            return builder.ToString();
        }

        return includeNamespace && !string.IsNullOrEmpty(type.Namespace)
            ? $"{type.Namespace}.{type.Name}"
            : type.Name;
    }

    private static bool TryGetAlias(Type type, out string alias)
    {
        alias = type switch
        {
            _ when type == typeof(byte) => "byte",
            _ when type == typeof(sbyte) => "sbyte",
            _ when type == typeof(short) => "short",
            _ when type == typeof(ushort) => "ushort",
            _ when type == typeof(int) => "int",
            _ when type == typeof(uint) => "uint",
            _ when type == typeof(long) => "long",
            _ when type == typeof(ulong) => "ulong",
            _ when type == typeof(float) => "float",
            _ when type == typeof(double) => "double",
            _ when type == typeof(decimal) => "decimal",
            _ when type == typeof(bool) => "bool",
            _ when type == typeof(char) => "char",
            _ when type == typeof(string) => "string",
            _ when type == typeof(object) => "object",
            _ => null!
        };

        return alias is not null;
    }
}

/// <summary>
/// Represents a single member hop in a <see cref="TypeInferencePath"/>.
/// </summary>
/// <param name="Owner">
/// The runtime type that declares the member.
/// </param>
/// <param name="MemberSuffix">
/// The member portion of the segment, for example <c>".Bar"</c> or <c>".Baz(input)"</c>.
/// </param>
internal readonly record struct TypeInferenceSegment(Type Owner, string MemberSuffix);

/// <summary>
/// Represents how an unresolved type reference was reached during type discovery.
/// The path can be rendered in a short form (aliases, no namespaces) for messages
/// and an expanded form (namespace-qualified) for tooling.
/// </summary>
internal readonly record struct TypeInferencePath
{
    private const string ArrowSeparator = " -> ";
    private const string TruncationMarker = "...";

    private readonly Type? _leafType;
    private readonly string? _leafText;
    private readonly IReadOnlyList<TypeInferenceSegment> _segments;
    private readonly bool _truncated;

    public TypeInferencePath(
        Type? leafType,
        string? leafText,
        IReadOnlyList<TypeInferenceSegment> segments,
        bool truncated)
    {
        _leafType = leafType;
        _leafText = leafText;
        _segments = segments;
        _truncated = truncated;
    }

    /// <summary>
    /// Gets the compact path using primitive aliases and no namespaces.
    /// </summary>
    public string Short => Render(includeNamespace: false);

    /// <summary>
    /// Gets the namespace-qualified path. Primitive aliases are preserved.
    /// </summary>
    public string Expanded => Render(includeNamespace: true);

    private string Render(bool includeNamespace)
    {
        var builder = new StringBuilder();

        if (_truncated)
        {
            builder.Append(TruncationMarker);
            builder.Append(ArrowSeparator);
        }

        foreach (var segment in _segments)
        {
            builder.Append(
                TypeInferencePathBuilder.GetFriendlyTypeName(segment.Owner, includeNamespace));
            builder.Append(segment.MemberSuffix);
            builder.Append(ArrowSeparator);
        }

        builder.Append(
            _leafType is null
                ? _leafText
                : TypeInferencePathBuilder.GetFriendlyTypeName(_leafType, includeNamespace));

        return builder.ToString();
    }
}
