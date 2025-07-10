using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Relay;
using HotChocolate.Utilities;
using static HotChocolate.Configuration.Validation.TypeValidationHelper;

#nullable enable

namespace HotChocolate.Configuration.Validation;

/// <summary>
/// Implements the object type validation defined in the spec.
/// https://spec.graphql.org/draft/#sec-Objects.Type-Validation
/// </summary>
internal sealed class ObjectTypeValidationRule : ISchemaValidationRule
{
    public void Validate(
        IDescriptorContext context,
        ISchemaDefinition schema,
        ICollection<ISchemaError> errors)
    {
        NodeType? nodeType = null;

        if (context.Options.StrictValidation)
        {
            if (context.Options.EnsureAllNodesCanBeResolved)
            {
                foreach (var type in schema.Types)
                {
                    if (type is NodeType nt)
                    {
                        nodeType = nt;
                        break;
                    }
                }
            }

            foreach (var type in schema.Types)
            {
                if (type is ObjectType objectType)
                {
                    EnsureTypeHasFields(objectType, errors);
                    EnsureFieldNamesAreValid(objectType, errors);
                    EnsureInterfacesAreCorrectlyImplemented(objectType, errors);
                    EnsureArgumentDeprecationIsValid(objectType, errors);

                    if (nodeType is not null && nodeType.IsAssignableFrom(objectType))
                    {
                        if (objectType.Features.Get<NodeTypeFeature>() is null)
                        {
                            errors.Add(ErrorHelper.NodeResolverMissing(objectType));
                        }
                    }
                }
            }
        }
    }
}
