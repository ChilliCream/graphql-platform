using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class EnumTypeConfig
    {
        public EnumTypeDefinitionNode SyntaxNode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public bool IsIntrospection { get; set; }

        public IEnumerable<EnumValueConfig> Values { get; set; }

        public virtual Type NativeType { get; set; }
    }
}
