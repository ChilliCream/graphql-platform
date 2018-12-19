using System;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Integration.ArgumentValidation
{
    public class ArgumentValidationDirective
    {
        public Action<IDirectiveContext, FieldNode, string, object> Validator
        {
            get;
            set;
        }
    }
}
