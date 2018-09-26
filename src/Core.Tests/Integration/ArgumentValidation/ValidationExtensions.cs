using System;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Integration.ArgumentValidation
{
    public static class ValidationExtensions
    {
        public static IArgumentDescriptor Validate<T>(
            this IArgumentDescriptor argumentDescriptor,
            Func<T, bool> func)
        {
            Action<IDirective, FieldNode, object> validator = (d, n, o) =>
            {
                bool isValid = false;
                if (o is T t)
                {
                    isValid = func(t);
                }

                if (!isValid)
                {
                    throw new QueryException(new ArgumentError(
                        "Argument is not valid.",
                        ((InputField)d.Source).Name,
                        n));
                }
            };

            return argumentDescriptor.Directive(
                new ArgumentValidationDirective { Validator = validator });
        }
    }
}
