using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

namespace HotChocolate.Configuration.Validation;

internal sealed class InterfaceHasAtLeastOneImplementationRule : ISchemaValidationRule
{
    public void Validate(
        IDescriptorContext context,
        ISchema schema,
        ICollection<ISchemaError> errors)
    {
        if (!context.Options.StrictValidation)
        {
            return;
        }

        var interfaceTypes = new HashSet<InterfaceType>();
        var fieldTypes = new HashSet<INamedType>();

        // first we get all interface types and add them to the interface type list.
        // we will strike from this list all the items that we find being implemented by
        // object types.
        foreach(var type in schema.Types)
        {
            if (type is InterfaceType interfaceType)
            {
                interfaceTypes.Add(interfaceType);
            }
        }

        // next we go through all the object types and strike the interfaces from the interface
        // list that we find being implemented.
        foreach(var type in schema.Types)
        {
            if (type is ObjectType objectType)
            {
                // we strike the interfaces that are being implemented.
                foreach (var interfaceType in objectType.Implements)
                {
                    interfaceTypes.Remove(interfaceType);
                }

                // we register all the interfaces that are being used as a field type.
                // if there are interfaces that are not being implemented and not being used by
                // fields they are removed by the cleanup of the schema, so we do not worry about
                // these.
                foreach (var field in objectType.Fields)
                {
                    if (field.Type.NamedType() is { Kind: TypeKind.Interface, } namedType)
                    {
                        fieldTypes.Add(namedType);
                    }
                }
            }
        }

        foreach (var interfaceType in interfaceTypes)
        {
            // if we do not remove unreachable types than all the interfaces left over here are
            // violations; otherwise, only the interfaces in the fieldTypes collection represent
            // violations.
            if (/* !options.RemoveUnreachableTypes || */ fieldTypes.Contains(interfaceType))
            {
                errors.Add(ErrorHelper.InterfaceHasNoImplementation(interfaceType));
            }
        }
    }
}
