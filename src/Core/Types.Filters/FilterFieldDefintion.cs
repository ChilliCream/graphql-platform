using System;
using System.Collections.Generic;
using System.Text;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public class FilterFieldDefintion : InputFieldDefinition
    { 
        public IBindableList<InputFieldDescriptor> Filters { get; }
           = new BindableList<InputFieldDescriptor>();
    }
}
