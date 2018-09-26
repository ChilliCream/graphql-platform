using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Integration.ArgumentValidation
{
    public class ArgumentValidationDirective
    {
        public Action<IDirective, FieldNode, object> Validator { get; set; }
    }
}
