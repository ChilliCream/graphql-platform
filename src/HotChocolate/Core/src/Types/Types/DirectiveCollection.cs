using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;

#nullable enable
namespace HotChocolate.Types;

/// <summary>
/// Represents a collection of directives of a <see cref="ITypeSystemMember"/>.
/// </summary>
public sealed class DirectiveCollection : IDirectiveCollection
{
    private readonly Directive[] _directives;

    private DirectiveCollection(Directive[] directives)
    {
        _directives = directives ?? throw new ArgumentNullException(nameof(directives));
    }

    /// <inheritdoc />
    public int Count => _directives.Length;

    /// <inheritdoc />
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

    /// <inheritdoc />
    public Directive this[int index] => _directives[index];

    /// <inheritdoc />
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

            // move pointer
#pragma warning disable CS8619
            start = ref Unsafe.Add(ref start, 1);
#pragma warning restore CS8619
        }

        return null;
    }

    /// <inheritdoc />
    public Directive? FirstOrDefault<TRuntimeType>()
    {
        var span = _directives.AsSpan();
        ref var start = ref MemoryMarshal.GetReference(span);
        ref var end = ref Unsafe.Add(ref start, span.Length);

        while (Unsafe.IsAddressLessThan(ref start, ref end))
        {
            if (start.AsValue<object>() is TRuntimeType)
            {
                return start;
            }

            // move pointer
#pragma warning disable CS8619
            start = ref Unsafe.Add(ref start, 1);
#pragma warning restore CS8619
        }

        return null;
    }

    /// <inheritdoc />
    public bool ContainsDirective(string directiveName)
        => FirstOrDefault(directiveName) is not null;

    /// <inheritdoc />
    public bool ContainsDirective<TRuntimeType>()
        => FirstOrDefault<TRuntimeType>() is not null;

    internal static DirectiveCollection CreateAndComplete(
        ITypeCompletionContext context,
        object source,
        IReadOnlyList<DirectiveDefinition> definitions)
    {
        var location = DirectiveHelper.InferDirectiveLocation(source);
        return CreateAndComplete(context, location, source, definitions);
    }

    internal static DirectiveCollection CreateAndComplete(
        ITypeCompletionContext context,
        DirectiveLocation location,
        object source,
        IReadOnlyList<DirectiveDefinition> definitions)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (definitions is null)
        {
            throw new ArgumentNullException(nameof(definitions));
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
                catch (SerializationException ex)
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

        // If we had any errors while building the directives list we will
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

    internal ReadOnlySpan<Directive> AsSpan()
        => _directives;

    internal ref Directive GetReference()
        => ref MemoryMarshal.GetArrayDataReference(_directives);

    /// <inheritdoc />
    public IEnumerator<Directive> GetEnumerator()
        => ((IEnumerable<Directive>)_directives).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
