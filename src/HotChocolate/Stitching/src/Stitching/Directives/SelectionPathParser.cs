using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Stitching;

internal static class SelectionPathParser
{
    private const int _maxStackSize = 256;

    public static IImmutableStack<SelectionPathComponent> Parse(string path)
    {
        throw new NotImplementedException();
    }
}
