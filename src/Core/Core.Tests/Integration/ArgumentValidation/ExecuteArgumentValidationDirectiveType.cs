using System.Collections.Generic;
using System.Linq;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Integration.ArgumentValidation
{
    public class ExecuteArgumentValidationDirectiveType
        : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("executeValidation");
            descriptor.Location(Types.DirectiveLocation.Object);
            descriptor.Use(next => context =>
            {
                Validate(context);
                return next.Invoke(context);
            });
        }

        public static void Validate(IDirectiveContext context)
        {
            foreach (Argument argument in context.Field.Arguments)
            {
                foreach (IDirective directive in argument.Directives)
                {
                    object argumentValue =
                        context.Argument<object>(argument.Name);

                    ArgumentValidationDirective argumentValidator =
                        directive.ToObject<ArgumentValidationDirective>();

                    argumentValidator.Validator(
                        context, context.FieldSelection,
                        argument.Name, argumentValue);
                }
            }
        }

        private static IEnumerable<IDirective> GetArgumentDirectives(ObjectField field)
        {
            return field.Arguments.SelectMany(t => t.Directives);
        }
    }
}
