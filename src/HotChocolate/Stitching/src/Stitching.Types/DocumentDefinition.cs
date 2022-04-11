using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Stitching.Types;

internal sealed class DocumentDefinition
{
    public DocumentDefinition()
    {
        Definition = Enumerable.Empty<ISchemaNode>();
    }

    public IEnumerable<ISchemaNode> Definition { get; set; }

    public void Add(ObjectTypeDefinition definition)
    {
        Definition = Definition.Concat(new[] { definition });
    }
}
