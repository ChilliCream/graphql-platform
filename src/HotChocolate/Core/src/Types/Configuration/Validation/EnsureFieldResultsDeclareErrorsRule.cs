using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

namespace HotChocolate.Configuration.Validation;

internal sealed class EnsureFieldResultsDeclareErrorsRule : ISchemaValidationRule
{
    private const string _errorKey = "HotChocolate.Types.Errors.ErrorConfigurations";

    public void Validate(
        IDescriptorContext context,
        ISchemaDefinition schema,
        ICollection<ISchemaError> errors)
    {
        var mutationType = schema.MutationType;

        foreach (var objectType in schema.Types.OfType<ObjectType>())
        {
            if (ReferenceEquals(objectType, mutationType))
            {
                continue;
            }

            foreach (var field in objectType.Fields.AsSpan())
            {
                var member = field.ResolverMember ?? field.Member;
                if (member is not null)
                {
                    var returnType = member.GetReturnType();
                    if (returnType is not { IsGenericType: true, GenericTypeArguments.Length: 1 })
                    {
                        continue;
                    }

                    var typeDefinition = returnType.GetGenericTypeDefinition();

                    if (typeDefinition == typeof(FieldResult<>))
                    {
                        EnsureErrorsAreDefined(field, errors);
                    }
                    else if (typeDefinition == typeof(ValueTask<>) || typeDefinition == typeof(Task<>))
                    {
                        var type = returnType.GenericTypeArguments[0];
                        if (type.IsGenericType
                            && type.GenericTypeArguments.Length == 1
                            && type.GetGenericTypeDefinition() == typeof(FieldResult<>))
                        {
                            EnsureErrorsAreDefined(field, errors);
                        }
                    }
                }
            }
        }
    }

    private static void EnsureErrorsAreDefined(
        ObjectField field,
        ICollection<ISchemaError> errors)
    {
        if (!field.ContextData.ContainsKey(_errorKey))
        {
            errors.Add(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "The field `{0}` must declare errors to use a FieldResult<T>.",
                        field.Coordinate)
                    .SetTypeSystemObject(field.DeclaringType)
                    .Build());
        }
    }
}
