using System;
using System.Collections.Generic;

namespace HotChocolate.Stitching
{
    public class SelectionPath
    {
        public SelectionPath(
            IReadOnlyCollection<SelectionPathComponent> components)
        {
            Components = components
                ?? throw new ArgumentNullException(nameof(components));
        }

        public IReadOnlyCollection<SelectionPathComponent> Components { get; }
    }
}
