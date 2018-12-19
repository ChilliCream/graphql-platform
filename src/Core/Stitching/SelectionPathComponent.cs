using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    public class SelectionPathComponent
    {
        public SelectionPathComponent(
            NameNode name,
            IReadOnlyCollection<ArgumentNode> arguments)
        {
            Name = name
                ?? throw new ArgumentNullException(nameof(name));
            Arguments = arguments
                ?? throw new ArgumentNullException(nameof(arguments));
        }

        public NameNode Name { get; }

        public IReadOnlyCollection<ArgumentNode> Arguments { get; }
    }
}
