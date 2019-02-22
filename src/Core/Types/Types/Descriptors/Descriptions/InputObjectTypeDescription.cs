using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    public class InputObjectTypeDescription
        : TypeDescriptionBase<InputObjectTypeDefinitionNode>
    {
        public IBindableList<InputFieldDescription> Fields { get; }
            = new BindableList<InputFieldDescription>();

        public BindingBehavior FieldBindingBehavior { get; set; }
    }
}
