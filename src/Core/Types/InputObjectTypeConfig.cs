using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class InputObjectTypeConfig
    {
        public InputObjectTypeDefinitionNode SyntaxNode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public Func<ITypeRegistry, Type> NativeType { get; set; }

        public IEnumerable<InputField> Fields { get; set; }
    }
}
