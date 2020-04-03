using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;

namespace HotChocolate.Configuration.Validation
{
    public class InterfaceTypeValidationRule : ISchemaValidationRule
    {
        public void Validate(
            IReadOnlyList<ITypeSystemObject> typeSystemObjects,
            IReadOnlySchemaOptions options,
            ICollection<ISchemaError> errors)
        {
            if (options.StrictValidation)
            {
                foreach (InterfaceType interfaceType in typeSystemObjects.OfType<InterfaceType>())
                {
                    if (interfaceType.Fields.Count == 0 ||
                        interfaceType.Fields.All(t => t.IsIntrospectionField))
                    {
                        // TODO : Resources
                        errors.Add();
                    }
                }
            }
        }
    }

    internal static class ComplexOutputTypeValidationHelper
    {





    }

    internal static class ErrorHelper
    {
        public static ISchemaError NeedsOneAtLeastField(IComplexOutputType type)
        {
            bool isInterface = type.Kind == TypeKind.Interface;

            return SchemaErrorBuilder.New()
                .SetMessage(
                    "The {0} type `{1}` has to at least define one field in " +
                    "order to be valid.",
                    isInterface ? "interface" : "object",
                    type.Name)
                .SetTypeSystemObject(type)
                .AddSyntaxNode(type.SyntaxNode)
                .SpecifiedBy("sec-Objects.Type-Validation")
                .Build()
        }
    }


    public enum ComplexOutputTypeKind
    {
        Object,
        Interface
    }
}
