using System.Collections.Generic;
using HotChocolate.Resolvers.Expressions.Parameters;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration.Validation;

internal sealed class IsSelectedPatternValidation : ISchemaValidationRule
{
    public void Validate(
        IDescriptorContext context,
        ISchema schema,
        ICollection<ISchemaError> errors)
    {
        if (!context.ContextData.TryGetValue(WellKnownContextData.PatternValidationTasks, out var value))
        {
            return;
        }

        foreach (var pattern in (List<IsSelectedPattern>)value!)
        {
            var objectField = schema.QueryType.Fields[pattern.FieldName];
            var validationContext = new ValidateIsSelectedPatternContext(schema, objectField);
            ValidateIsSelectedPatternVisitor.Instance.Visit(pattern.Pattern, validationContext);

            if (validationContext.Error is not null)
            {
                errors.Add(validationContext.Error);
            }
        }
    }
}
