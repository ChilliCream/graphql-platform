using System.Collections.Generic;
using System.Linq;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Integration.ArgumentValidation
{
    public class ExecuteArgumentValidationMiddleware
    {
        public void Validate(IResolverContext context)
        {
            foreach (InputField argument in context.Field.Arguments)
            {
                foreach (IDirective directive in argument.Directives)
                {
                    object argumentValue =
                        context.Argument<object>(argument.Name);

                    var argumentValidator =
                        directive.ToObject<ArgumentValidationDirective>();

                    argumentValidator.Validator(
                        directive, context.FieldSelection, argumentValue);
                }
            }
        }

        private IEnumerable<IDirective> GetArgumentDirectives(ObjectField field)
        {
            return field.Arguments.SelectMany(t => t.Directives);
        }
    }
}
