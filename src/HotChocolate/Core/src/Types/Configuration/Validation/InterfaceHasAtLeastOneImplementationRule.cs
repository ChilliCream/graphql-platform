using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HotChocolate.Types;

namespace HotChocolate.Configuration.Validation;

internal class InterfaceHasAtLeastOneImplementationRule
    : ISchemaValidationRule
{
    public void Validate(
        IReadOnlyList<ITypeSystemObject> typeSystemObjects,
        IReadOnlySchemaOptions options,
        ICollection<ISchemaError> errors)
    {
        if (!options.StrictValidation)
        {
            return;
        }

        var interfaceTypes = new HashSet<InterfaceType>(
            typeSystemObjects.OfType<InterfaceType>());

        var fieldTypes = new HashSet<INamedType>();

        foreach (var objectType in typeSystemObjects.OfType<ObjectType>())
        {
            foreach (var interfaceType in objectType.Implements)
            {
                interfaceTypes.Remove(interfaceType);
            }

            foreach (var field in objectType.Fields)
            {
                fieldTypes.Add(field.Type.NamedType());
            }
        }

        foreach (var interfaceType in interfaceTypes.Where(fieldTypes.Contains))
        {
            // TODO : resources
            errors.Add(SchemaErrorBuilder.New()
                .SetMessage(string.Format(
                    CultureInfo.InvariantCulture,
                    "There is no object type implementing interface `{0}`.",
                    interfaceType.Name.Value))
                .SetCode(ErrorCodes.Schema.InterfaceNotImplemented)
                .SetTypeSystemObject(interfaceType)
                .AddSyntaxNode(interfaceType.SyntaxNode)
                .Build());
        }
    }
}
