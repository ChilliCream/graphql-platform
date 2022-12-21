using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;

#nullable enable
namespace HotChocolate.Types;

public sealed class DirectiveCollection : IDirectiveCollection
{
    private readonly object _source;
    private readonly DirectiveLocation _location;
    private readonly Directive[] _directives;

    private DirectiveCollection(object source, Directive[] directives)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _directives = directives ?? throw new ArgumentNullException(nameof(directives));
        _location = DirectiveHelper.InferDirectiveLocation(source);
    }

    public int Count => _directives.Length;

    public IEnumerable<Directive> this[string directiveName]
    {
        get
        {
            var directives = _directives;
            return directives.Length == 0
                ? Enumerable.Empty<Directive>()
                : FindDirectives(directives, directiveName);
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

    public Directive this[int index] => _directives[index];

    public Directive? FirstOrDefault(string directiveName)
    {
        directiveName.EnsureGraphQLName();

        var span = _directives.AsSpan();
        ref var start = ref MemoryMarshal.GetReference(span);
        ref var end = ref Unsafe.Add(ref start, span.Length);

        while (Unsafe.IsAddressLessThan(ref start, ref end))
        {
            var directive = Unsafe.Add(ref start, 0);

            if (directive.Type.Name.EqualsOrdinal(directiveName))
            {
                return directive;
            }

            // move pointer
            start = ref Unsafe.Add(ref start, 1);
        }

        return null;
    }

    public bool ContainsDirective(string directiveName)
        => FirstOrDefault(directiveName) is not null;

    internal static DirectiveCollection CreateAndComplete(
        ITypeCompletionContext context,
        object source,
        IReadOnlyList<DirectiveDefinition>? definitions)
    {
        var directives = new Directive[definitions.Count];

        for (var i = 0; i < directives.Length; i++)
        {
            var definition = definitions[i];
            var value = definition.Value;

            if (context.TryGetDirectiveType(definition.Type, out var directiveType))
            {
                directives[i] = value is DirectiveNode syntaxNode
                    ? new Directive(directiveType, syntaxNode)
                    : new Directive(directiveType, directiveType.Format(value), value);
            }
        }

        return new DirectiveCollection(source, directives);
    }

    internal ReadOnlySpan<Directive> AsSpan()
        => _directives;

    internal ref Directive GetReference()
#if NET6_0_OR_GREATER
        => ref MemoryMarshal.GetArrayDataReference(_directives);
#else
        => ref MemoryMarshal.GetReference(_directives.AsSpan());
#endif

    public IEnumerator<Directive> GetEnumerator()
        => ((IEnumerable<Directive>)_directives).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
