using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HotChocolate.Types;

namespace HotChocolate.Configuration.Validation
{
    internal class InterfaceHasAtLeastOneImplementationRule
        : ISchemaValidationRule
    {
        public IEnumerable<ISchemaError> Validate(
            IReadOnlyList<ITypeSystemObject> typeSystemObjects,
            IReadOnlySchemaOptions options)
        {
            if (!options.StrictValidation)
            {
                yield break;
            }

            var interfaceTypes = new HashSet<InterfaceType>(
                typeSystemObjects.OfType<InterfaceType>());

            foreach (ObjectType objectType in typeSystemObjects.OfType<ObjectType>())
            {
                foreach (InterfaceType interfaceType in objectType.Interfaces.Values)
                {
                    interfaceTypes.Remove(interfaceType);
                }
            }

            foreach (InterfaceType interfaceType in interfaceTypes)
            {
                // TODO : resources
                yield return SchemaErrorBuilder.New()
                    .SetMessage(string.Format(
                        CultureInfo.InvariantCulture,
                        "There is no object type implementing interface `{0}`.",
                        interfaceType.Name.Value))
                    .SetCode("SCHEMA_INTERFACE_NO_IMPL")
                    .SetTypeSystemObject(interfaceType)
                    .AddSyntaxNode(interfaceType.SyntaxNode)
                    .Build();
            }
        }
    }
}
