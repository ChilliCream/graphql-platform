using System;
using HotChocolate.Language;
using HotChocolate.Resolvers;

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
