using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class InputObjectTypeDefinition
        : TypeDefinitionBase<InputObjectTypeDefinitionNode>
    {
        public IBindableList<InputFieldDefinition> Fields { get; }
            = new BindableList<InputFieldDefinition>();

        public IEnumerable<ITypeReference> GetDependencies()
        {
            var dependencies = new List<ITypeReference>();

            foreach (InputFieldDefinition field in Fields)
            {
                dependencies.Add(field.Type);
            }

            return dependencies;
        }
    }
}
