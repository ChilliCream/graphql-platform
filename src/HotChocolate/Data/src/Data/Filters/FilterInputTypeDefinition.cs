using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    public class FilterInputTypeDefinition
        : TypeDefinitionBase<InputObjectTypeDefinitionNode>
    {
        public IBindableList<InputFieldDefinition> Fields { get; } =
            new BindableList<InputFieldDefinition>();

        public Type? EntityType { get; set; }

    }
}