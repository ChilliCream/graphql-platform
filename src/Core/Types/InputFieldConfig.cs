using System;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class InputFieldConfig
    {
        public InputValueDefinitionNode SyntaxNode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public PropertyInfo Property { get; set; }

        public Func<ITypeRegistry, IInputType> Type { get; set; }

        public Type NativeNamedType { get; set; }

        public Func<ITypeRegistry, IValueNode> DefaultValue { get; set; }
    }
}
