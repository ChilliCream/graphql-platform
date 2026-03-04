using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;

namespace HotChocolate.Types;

/// <summary>
/// Represents a collection of directives of a <see cref="ITypeSystemMember"/>.
/// </summary>
public sealed class DirectiveCollection : IReadOnlyList<Directive>
{
    private readonly Directive[] _directives;
    private IReadOnlyDirectiveCollection? _wrapper;

    private DirectiveCollection(Directive[] directives)
    {
        _directives = directives ?? throw new ArgumentNullException(nameof(directives));
    }

    /// <inheritdoc />
    public int Count => _directives.Length;

    /// <summary>
    /// Gets all directives of a certain directive definition.
    /// </summary>
    /// <param name="directiveName">
    /// The name of the directive definition.
    /// </param>
    public IEnumerable<Directive> this[string directiveName]
    {
        get
        {
            var directives = _directives;
            return directives.Length == 0 ? [] : FindDirectives(directives, directiveName);
        }
    }

    private static IEnumerable<Directive> FindDirectives(Directive[] directives, string name)
    {
        for (var i = 0; i < directives.Length; i++)
        {
            var directive = directives[i];

            if (directive.Type.Name.EqualsOrdinal(name))
            {
                yield return directive;
            }
        }
    }

    /// <summary>
    /// Gets the directive at the specified index.
    /// </summary>
    /// <param name="index">
    /// The index of the directive to get.
    /// </param>
    public Directive this[int index] => _directives[index];

    /// <summary>
    /// Gets the first directive that matches the specified name.
    /// </summary>
    /// <param name="directiveName">
    /// The name of the directive.
    /// </param>
    /// <returns>
    /// The first directive that matches the specified name.
    /// </returns>
    public Directive? FirstOrDefault(string directiveName)
    {
        directiveName.EnsureGraphQLName();

        var span = _directives.AsSpan();
        ref var start = ref MemoryMarshal.GetReference(span);
        ref var end = ref Unsafe.Add(ref start, span.Length);

        while (Unsafe.IsAddressLessThan(ref start, ref end))
        {
            if (start.Type.Name.EqualsOrdinal(directiveName))
            {
                return start;
            }

#pragma warning disable CS8619
            start = ref Unsafe.Add(ref start, 1);
#pragma warning restore CS8619
        }

        return null;
    }

    /// <summary>
    /// Gets the first directive that matches the specified runtime type.
    /// </summary>
    /// <param name="runtimeType">
    /// The runtime type of the directive.
    /// </param>
    /// <returns>
    /// The first directive that matches the specified runtime type.
    /// </returns>
    public Directive? FirstOrDefault(Type runtimeType)
    {
        ArgumentNullException.ThrowIfNull(runtimeType);

        var span = _directives.AsSpan();
        ref var start = ref MemoryMarshal.GetReference(span);
        ref var end = ref Unsafe.Add(ref start, span.Length);

        while (Unsafe.IsAddressLessThan(ref start, ref end))
        {
            if (start.Type.RuntimeType == runtimeType)
            {
                return start;
            }

            start = ref Unsafe.Add(ref start, 1)!;
        }

        return null;
    }

    /// <summary>
    /// Gets the first directive that matches the specified runtime type.
    /// </summary>
    /// <typeparam name="TRuntimeType">
    /// The runtime type of the directive.
    /// </typeparam>
    /// <returns>
    /// The first directive that matches the specified runtime type.
    /// </returns>
    public Directive? FirstOrDefault<TRuntimeType>()
        => FirstOrDefault(typeof(TRuntimeType));

    /// <summary>
    /// Determines whether the collection contains a directive with the specified name.
    /// </summary>
    /// <param name="directiveName">
    /// The name of the directive.
    /// </param>
    /// <returns>
    /// <c>true</c>, if the collection contains a directive with the specified name;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool ContainsDirective(string directiveName)
        => FirstOrDefault(directiveName) is not null;

    /// <summary>
    /// Determines whether the collection contains a directive with the specified runtime type.
    /// </summary>
    /// <typeparam name="TRuntimeType">
    /// The runtime type of the directive.
    /// </typeparam>
    /// <returns>
    /// <c>true</c>, if the collection contains a directive with the specified runtime type;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool ContainsDirective<TRuntimeType>()
        => FirstOrDefault<TRuntimeType>() is not null;

    internal static DirectiveCollection CreateAndComplete(
        ITypeCompletionContext context,
        object source,
        IReadOnlyList<DirectiveConfiguration> definitions)
    {
        var location = DirectiveHelper.InferDirectiveLocation(source);
        return CreateAndComplete(context, location, source, definitions);
    }

    internal static DirectiveCollection CreateAndComplete(
        ITypeCompletionContext context,
        DirectiveLocation location,
        object source,
        IReadOnlyList<DirectiveConfiguration> definitions)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Count == 0)
        {
            return Empty;
        }

        var directives = new Directive[definitions.Count];
        var directiveNames = TypeMemHelper.RentNameSet();
        var hasErrors = false;

        for (var i = 0; i < directives.Length; i++)
        {
            var definition = definitions[i];
            var value = definition.Value;

            if (context.TryGetDirectiveType(definition.Type, out var directiveType))
            {
                if ((directiveType.Locations & location) != location)
                {
                    hasErrors = true;

                    var directiveNode = definition.Value as DirectiveNode;

                    context.ReportError(
                        ErrorHelper.DirectiveCollection_LocationNotAllowed(
                            directiveType,
                            location,
                            context.Type,
                            directiveNode,
                            source));
                    continue;
                }

                if (!directiveNames.Add(directiveType.Name) && !directiveType.IsRepeatable)
                {
                    hasErrors = true;

                    var directiveNode = definition.Value as DirectiveNode;

                    context.ReportError(
                        ErrorHelper.DirectiveCollection_DirectiveIsUnique(
                            directiveType,
                            context.Type,
                            directiveNode,
                            source));
                    continue;
                }

                DirectiveNode? syntaxNode = null;
                object? runtimeValue = null;

                try
                {
                    // We will parse or format the directive.
                    // This will also validate the directive instance values which could
                    // cause and error.
                    if (value is DirectiveNode node)
                    {
                        syntaxNode = node;
                        runtimeValue = directiveType.Parse(node);
                    }
                    else
                    {
                        syntaxNode = directiveType.Format(value);
                        runtimeValue = value;
                    }
                }
                catch (LeafCoercionException ex)
                {
                    hasErrors = true;

                    Debug.Assert(
                        ex.Path is not null,
                        "The path is always passed in with directives!");

                    context.ReportError(
                        ErrorHelper.DirectiveCollection_ArgumentError(
                            directiveType,
                            syntaxNode,
                            source,
                            ex.Path,
                            ex));
                }

                if (syntaxNode is not null && runtimeValue is not null)
                {
                    directives[i] = new Directive(directiveType, syntaxNode, runtimeValue);
                }
            }
        }

        // If we had any errors while building the directive list, we will
        // clean the null entries out so that the list is consistent.
        // We only do that, so we can collect other schema errors as well and do
        // not have to fully fail here but have one SchemaException at the end of
        // the schema creation that contains a list of errors.
        if (hasErrors)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            directives = directives.Where(t => t is not null).ToArray();
        }

        return new DirectiveCollection(directives);
    }

    internal IReadOnlyDirectiveCollection AsReadOnlyDirectiveCollection()
        => _wrapper ??= new ReadOnlyDirectiveCollection(this);

    internal ReadOnlySpan<Directive> AsSpan()
        => _directives;

    internal ref Directive GetReference()
        => ref MemoryMarshal.GetArrayDataReference(_directives);

    public IEnumerator<Directive> GetEnumerator()
        => Unsafe.As<IEnumerable<Directive>>(_directives).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    internal static DirectiveCollection Empty { get; } = new([]);

    private sealed class ReadOnlyDirectiveCollection(DirectiveCollection directives) : IReadOnlyDirectiveCollection
    {
        public int Count => directives._directives.Length;

        public IDirective this[int index]
            => directives._directives[index];

        public IEnumerable<IDirective> this[string directiveName]
            => directives[directiveName];

        public IDirective? FirstOrDefault(string directiveName)
            => directives.FirstOrDefault(directiveName);

        public IDirective? FirstOrDefault(Type runtimeType)
            => directives.FirstOrDefault(runtimeType);

        public bool ContainsName(string directiveName)
            => directives.ContainsDirective(directiveName);

        public IEnumerator<IDirective> GetEnumerator()
            => directives.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
